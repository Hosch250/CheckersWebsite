using CheckersWebsite.SignalR;
using Microsoft.AspNetCore.SignalR;
using Project.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CheckersWebsite.Facade;
using System.Threading;

namespace CheckersWebsite.Controllers
{
    public class ComputerPlayer
    {
        public static Guid ComputerPlayerID { get; } = new Guid("BB04EFBB-77B1-4EE3-879D-197B3A6B14BF");

        private readonly Database.Context _context;
        private readonly IHubContext<SignalRHub> _signalRHub;
        private readonly IViewRenderService _viewRenderService;

        public ComputerPlayer(Database.Context context,
            IHubContext<SignalRHub> signalRHub,
            IViewRenderService viewRenderService)
        {
            _context = context;
            _signalRHub = signalRHub;
            _viewRenderService = viewRenderService;
        }

        private string GetClientConnection(Guid id)
        {
            return _context.Players.Find(id).ConnectionID;
        }

        internal async Task DoComputerMove(Guid id)
        {
            var game = _context.Games.FirstOrDefault(f => f.ID == id);

            if (game.CurrentPlayer == (int)Player.Black && game.BlackPlayerID != ComputerPlayerID ||
                game.CurrentPlayer == (int)Player.White && game.WhitePlayerID != ComputerPlayerID)
            {
                return;
            }

            var controller = game.ToGame();
            var move = controller.Move(controller.GetMove(game.CurrentPlayer == (int)Player.Black ? game.BlackPlayerStrength : game.WhitePlayerStrength, CancellationToken.None));
            move.ID = game.ID;

            var turn = move.MoveHistory.Last().ToPdnTurn();
            if (game.Turns.Any(t => t.MoveNumber == turn.MoveNumber))
            {
                var recordedTurn = game.Turns.Single(s => s.MoveNumber == turn.MoveNumber);
                Database.PdnMove newMove;
                switch (controller.CurrentPlayer)
                {
                    case Player.White:
                        newMove = move.MoveHistory.Last().WhiteMove.ToPdnMove();
                        break;
                    case Player.Black:
                        newMove = move.MoveHistory.Last().BlackMove.ToPdnMove();
                        break;
                    default:
                        throw new ArgumentException();
                }

                var existingMove = recordedTurn.Moves.FirstOrDefault(a => a.Player == (int)controller.CurrentPlayer);
                if (existingMove != null)
                {
                    recordedTurn.Moves.Remove(existingMove);
                }
                recordedTurn.Moves.Add(newMove);

                game.Fen = newMove.ResultingFen;
            }
            else
            {
                game.Turns.Add(move.MoveHistory.Last().ToPdnTurn());
                game.Fen = turn.Moves.Single().ResultingFen;
            }

            game.CurrentPosition = move.GetCurrentPosition();
            game.CurrentPlayer = (int)move.CurrentPlayer;
            game.GameStatus = (int)move.GetGameStatus();

            _context.SaveChanges();

            Dictionary<string, object> GetViewData(Guid localPlayerID, Player orientation)
            {
                return new Dictionary<string, object>
                {
                    ["playerID"] = localPlayerID,
                    ["blackPlayerID"] = game.BlackPlayerID,
                    ["whitePlayerID"] = game.WhitePlayerID,
                    ["orientation"] = orientation
                };
            }

            var moveHistory = await _viewRenderService.RenderToStringAsync("Controls/MoveControl", move.MoveHistory, new Dictionary<string, object>());

            if (game.BlackPlayerID != ComputerPlayerID)
            {
                _signalRHub.Clients.Client(GetClientConnection(game.BlackPlayerID)).InvokeAsync("UpdateBoard", id,
                    await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, GetViewData(game.BlackPlayerID, Player.Black)),
                    await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, GetViewData(game.BlackPlayerID, Player.White)));
            }

            if (game.WhitePlayerID != ComputerPlayerID)
            {
                _signalRHub.Clients.Client(GetClientConnection(game.WhitePlayerID)).InvokeAsync("UpdateBoard", id,
                    await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, GetViewData(game.WhitePlayerID, Player.Black)),
                    await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, GetViewData(game.WhitePlayerID, Player.White)));
            }

            _signalRHub.Clients.AllExcept(new List<string> { GetClientConnection(game.BlackPlayerID), GetClientConnection(game.WhitePlayerID) }).InvokeAsync("UpdateBoard", id,
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, GetViewData(Guid.Empty, Player.Black)),
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, GetViewData(Guid.Empty, Player.White)));

            _signalRHub.Clients.All.InvokeAsync("UpdateMoves", moveHistory);
            _signalRHub.Clients.All.InvokeAsync("UpdateOpponentState", ((Player)game.CurrentPlayer).ToString(), move.GetGameStatus().ToString());

            if (game.Turns.Count == 1 && game.Turns.ElementAt(0).Moves.Count == 1)
            {
                _signalRHub.Clients.All.InvokeAsync("SetAttribute", "undo", "disabled", "");
            }
            else
            {
                _signalRHub.Clients.All.InvokeAsync("RemoveAttribute", "undo", "disabled");
            }

            _signalRHub.Clients.All.InvokeAsync(move.IsDrawn() || move.IsWon() ? "RemoveClass" : "AddClass", "new-game", "hide");
            _signalRHub.Clients.All.InvokeAsync(move.IsDrawn() || move.IsWon() ? "AddClass" : "RemoveClass", "resign", "hide");

            Task.Run(() => DoComputerMove(id));
            return;
        }
    }
}
