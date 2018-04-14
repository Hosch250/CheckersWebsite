using CheckersWebsite.Facade;
using CheckersWebsite.SignalR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheckersWebsite.Controllers
{
    public class BoardController : Controller
    {
        private readonly Database.Context _context;
        private readonly IHubContext<MovesHub> _movesHub;
        private readonly IHubContext<OpponentsHub> _opponentsHub;

        public BoardController(Database.Context context,
            IHubContext<MovesHub> movesHub,
            IHubContext<OpponentsHub> opponentsHub)
        {
            _context = context;
            _movesHub = movesHub;
            _opponentsHub = opponentsHub;
        }

        public ActionResult MovePiece(Guid id, Coord start, Coord end)
        {
            var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.ID == id);
            
            var controller = game?.ToGame()
                ?? GameController.FromVariant(Variant.AmericanCheckers);

            if (!controller.IsValidMove(start, end))
            {
                Response.StatusCode = 403;
                return null;
            }

            var move = controller.Move(start, end);
            if (game == null || id == Guid.Empty)
            {
                move.ID = Guid.NewGuid();
                game = move.ToGame();
                _context.Games.Add(game);
            }
            else
            {
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
            }

            _context.SaveChanges();
            
            _movesHub.Clients.All.InvokeAsync("Update", BuildMoveHistory.GetHtml(game.Turns.Select(s => s.ToPdnTurn()).ToList()));
            _opponentsHub.Clients.All.InvokeAsync("Update", ((Player)game.CurrentPlayer).ToString());

            return PartialView("~/Views/Controls/CheckersBoard.cshtml", move);
        }

        public ActionResult Undo(Guid id)
        {
            var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.ID == id);

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

            _context.SaveChanges();

            _movesHub.Clients.All.InvokeAsync("Update", BuildMoveHistory.GetHtml(game.Turns.Select(s => s.ToPdnTurn()).ToList()));
            _opponentsHub.Clients.All.InvokeAsync("Update", ((Player)game.CurrentPlayer).ToString());

            var controller = game.ToGame();
            controller.ID = id;
            return PartialView("~/Views/Controls/CheckersBoard.cshtml", controller);
        }
    }

    public class BuildMoveHistory
    {
        public static string GetHtml(List<PdnTurn> turns)
        {
            var stringWriter = new StringWriter();
            Action<string> write = stringWriter.Write;

            write(@"<ol class=""moves"">");

            foreach (var turn in turns)
            {
                write("<li>");
                write($@"<input id=""{turn.MoveNumber}-black-move"" class=""toggle"" name=""move"" type=""radio"" value=""{turn.BlackMove.DisplayString}"" /> <label for=""{turn.MoveNumber}-black-move"">{turn.BlackMove.DisplayString}</label>");

                if (turn.WhiteMove != null)
                {
                    write($@"<input id=""{turn.MoveNumber}-white-move"" class=""toggle"" name=""move"" type=""radio"" value=""{turn.WhiteMove.DisplayString}"" /> <label for=""{turn.MoveNumber}-white-move"">{turn.WhiteMove.DisplayString}</label>");
                }
                write("</li>");
            }
            write("</ol>");

            return stringWriter.ToString();
        }
    }
}