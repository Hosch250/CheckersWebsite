using System;
using System.Linq;
using CheckersWebsite.Extensions;
using System.Threading;
using CheckersWebsite.Enums;
using MediatR;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CheckersWebsite.Controllers
{
    public class ComputerPlayer
    {
        public static Guid ComputerPlayerID { get; } = new Guid("BB04EFBB-77B1-4EE3-879D-197B3A6B14BF");

        private readonly Database.Context _context;
        private readonly IMediator _mediator;

        public ComputerPlayer(Database.Context context,
            IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        private string GetClientConnection(Guid id)
        {
            return _context.Players.Find(id).ConnectionID;
        }

        internal Task DoComputerMove(Guid id)
        {
            return Task.Run(() =>
            {
                var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.ID == id);

                if (game.GameStatus != (int)Status.InProgress)
                {
                    return;
                }

                if (game.CurrentPlayer == (int)Player.Black && game.BlackPlayerID != ComputerPlayerID ||
                    game.CurrentPlayer == (int)Player.White && game.WhitePlayerID != ComputerPlayerID)
                {
                    return;
                }

                var controller = game.ToGameController();
                var move = controller.Move(controller.GetMove(game.CurrentPlayer == (int)Player.Black ? game.BlackPlayerStrength : game.WhitePlayerStrength, CancellationToken.None));
                move.ID = game.ID;

                var turn = move.MoveHistory.Last().ToPdnTurn();
                if (game.Turns != null && game.Turns.Any(t => t.MoveNumber == turn.MoveNumber))
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
                game.RowVersion = DateTime.Now;

                _context.SaveChanges();

                _mediator.Publish(new OnMoveNotification(game.ToGameViewModel())).Wait();
            });
        }
    }
}
