using CheckersWebsite.Facade;
using CheckersWebsite.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using CheckersWebsite.Views.Controls;
using CheckersWebsite.Enums;
using CheckersWebsite.Extensions;
using MediatR;
using CheckersWebsite.Actions.MoveActions;
using CheckersWebsite.Actions.GameJoinedActions;
using CheckersWebsite.Actions.GameCompletedActions;

namespace CheckersWebsite.Controllers
{
    public class BoardController : Controller
    {
        private readonly IMediator _mediator;
        private readonly Database.Context _context;
        private readonly IHubContext<GameHub> _signalRHub;
        private readonly ComputerPlayer _computerPlayer;

        public BoardController(Database.Context context,
            IHubContext<GameHub> signalRHub,
            ComputerPlayer computerPlayer,
            IMediator mediator)
        {
            _context = context;
            _signalRHub = signalRHub;
            _computerPlayer = computerPlayer;
            _mediator = mediator;
        }

        private Theme GetThemeOrDefault()
        {
            if (Request.Cookies.Keys.All(a => a != "theme"))
            {
                return Theme.Steel;
            }

            return Enum.Parse(typeof(Theme), Request.Cookies["theme"]) as Theme? ?? Theme.Steel;
        }

        private Guid? GetPlayerID()
        {
            if (Request.Cookies.TryGetValue("playerID", out var id))
            {
                return Guid.Parse(id);
            }

            return null;
        }

        private string GetClientConnection(Guid id)
        {
            return _context.Players.Find(id).ConnectionID;
        }

        public ActionResult MovePiece(Guid id, Coord start, Coord end)
        {
            var playerID = GetPlayerID();
            if (!playerID.HasValue)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.ID == id);

            if (game == null ||
                game.GameStatus != (int)Status.InProgress ||
                (game.BlackPlayerID != playerID && game.CurrentPlayer == (int)Player.Black) ||
                (game.WhitePlayerID != playerID && game.CurrentPlayer == (int)Player.White))
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var controller = game.ToGameController();

            if (!controller.IsValidMove(start, end))
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var move = controller.Move(start, end);
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
            
            game.RowVersion = DateTime.Now;
            _context.SaveChanges();

            var viewModel = game.ToGameViewModel();
            _mediator.Publish(new OnMoveNotification(viewModel)).Wait();
            
            return Content("");
        }

        public ActionResult Undo(Guid id)
        {
            var playerID = GetPlayerID();
            if (!playerID.HasValue)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.ID == id);

            if (game == null ||
                game.GameStatus != (int)Status.InProgress ||
                game.BlackPlayerID != playerID && game.WhitePlayerID != playerID ||
                game.BlackPlayerID == ComputerPlayer.ComputerPlayerID ||
                game.WhitePlayerID == ComputerPlayer.ComputerPlayerID)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var lastTurn = game.Turns.OrderBy(a => a.MoveNumber).Last();

            if (lastTurn.Moves.Count == 2)
            {
                // todo: figure out which is the last move based on variant
                lastTurn.Moves.Remove(lastTurn.Moves.Single(s => (Player)s.Player == Player.White));
                game.Fen = game.Turns.Last().Moves.Single(s => (Player)s.Player == Player.Black).ResultingFen;
            }
            else
            {
                if (game.Turns.Count == 1)
                {
                    Response.StatusCode = 403;
                    return null;
                }

                game.Turns.Remove(lastTurn);
                game.Fen = game.Turns.Last().Moves.Single(s => (Player)s.Player == Player.White).ResultingFen;
            }

            game.CurrentPosition = -1;

            switch ((Player)game.CurrentPlayer)
            {
                case Player.White:
                    game.CurrentPlayer = (int)Player.Black;
                    break;
                case Player.Black:
                    game.CurrentPlayer = (int)Player.White;
                    break;
                default:
                    break;
            }
            
            game.RowVersion = DateTime.Now;
            _context.SaveChanges();

            _mediator.Publish(new OnMoveNotification(game.ToGameViewModel())).Wait();
            return Content("");
        }

        public ActionResult Resign(Guid id)
        {
            var playerID = GetPlayerID();
            if (!playerID.HasValue)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var game = _context.Games.FirstOrDefault(f => f.ID == id);
            if (game == null ||
                game.GameStatus != (int) Status.InProgress ||
                game.BlackPlayerID != playerID && game.WhitePlayerID != playerID)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            if (game.BlackPlayerID == Guid.Empty || game.WhitePlayerID == Guid.Empty)
            {
                game.GameStatus = (int)Status.Aborted;
            }
            else
            {
                game.GameStatus = playerID == game.BlackPlayerID ? (int)Status.WhiteWin : (int)Status.BlackWin;
            }

            game.RowVersion = DateTime.Now;
            _context.SaveChanges();

            _mediator.Publish(new OnGameCompletedNotification(game.ToGameViewModel()));

            return Content("");
        }

        public ActionResult DisplayGame(Guid moveID, Player orientation)
        {
            var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.Turns.Any(a => a.Moves.Any(m => m.ID == moveID)));

            if (game == null)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var move = game.Turns.SelectMany(t => t.Moves).First(f => f.ID == moveID);

            var viewData = new Dictionary<string, object>
            {
                ["playerID"] = GetPlayerID(),
                ["orientation"] = orientation
            };

            var controller = GameController.FromPosition((Variant) game.Variant, move.ResultingFen);

            var viewModel = game.ToGameViewModel();
            viewModel.Board.GameBoard = controller.Board.GameBoard;
            viewModel.DisplayingLastMove = false;

            var board = ComponentGenerator.GetBoard(viewModel, viewData).Replace("[theme]", GetThemeOrDefault().ToString());
            return Content(board);
        }

        public ActionResult Join(Guid id)
        {
            var playerID = GetPlayerID();
            if (!playerID.HasValue)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var game = _context.Games.FirstOrDefault(f => f.ID == id);
            if (game.BlackPlayerID != Guid.Empty && game.WhitePlayerID != Guid.Empty ||
                game.BlackPlayerID == playerID ||
                game.WhitePlayerID == playerID)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            if (game.BlackPlayerID == Guid.Empty)
            {
                game.BlackPlayerID = playerID.Value;
            }
            else if (game.WhitePlayerID == Guid.Empty)
            {
                game.WhitePlayerID = playerID.Value;
            }

            game.RowVersion = DateTime.Now;
            try
            {
                _context.SaveChanges();
                var gameViewModel = game.ToGameViewModel();
                _mediator.Publish(new OnGameJoinedNotification(gameViewModel, playerID.Value)).Wait();

                var viewData = new Dictionary<string, object>
                {
                    ["playerID"] = playerID,
                    ["orientation"] = game.BlackPlayerID == playerID ? Player.Black : Player.White,
                };

                var board = ComponentGenerator.GetBoard(gameViewModel, viewData).Replace("[theme]", GetThemeOrDefault().ToString());
                return Content(board);
            }
            catch (DbUpdateConcurrencyException)
            {
                _signalRHub.Clients.All.SendAsync("AddClass", "join", "hide");

                Response.StatusCode = 403;
                return Content(Resources.Resources.GameJoined);
            }
        }

        public ActionResult Orientate(Guid id, Guid? moveID, Player orientation)
        {
            var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.ID == id);

            if (game == null)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var move = game.Turns.SelectMany(t => t.Moves).FirstOrDefault(f => f.ID == moveID) ??
                game.Turns.OrderBy(o => o.MoveNumber).LastOrDefault()?.Moves.OrderBy(a => a.CreatedOn).LastOrDefault();
            
            Dictionary<string, object>
                viewData = new Dictionary<string, object>
                {
                    ["playerID"] = GetPlayerID(),
                    ["orientation"] = orientation
                };

            var viewModel = game.ToGameViewModel();
            if (moveID != null && moveID.Value != game.Turns.Last().Moves.OrderBy(o => o.CreatedOn).Last().ID)
            {
                var fen = move.ResultingFen;
                var controller = GameController.FromPosition((Variant)game.Variant, fen);

                viewModel.Board.GameBoard = controller.Board.GameBoard;
                viewModel.DisplayingLastMove = false;
            }

            var board = ComponentGenerator.GetBoard(viewModel, viewData).Replace("[theme]", GetThemeOrDefault().ToString());
            return Content(board);
        }
    }
}