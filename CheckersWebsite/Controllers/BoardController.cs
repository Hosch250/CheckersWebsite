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
        private readonly IHubContext<BoardHub> _boardHub;
        private readonly IHubContext<OpponentsHub> _opponentsHub;

        public BoardController(Database.Context context,
            IHubContext<MovesHub> movesHub,
            IHubContext<BoardHub> boardHub,
            IHubContext<OpponentsHub> opponentsHub)
        {
            _context = context;
            _movesHub = movesHub;
            _boardHub = boardHub;
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
                return Content("");
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
                game.GameStatus = (int)move.GameStatus();
            }

            _context.SaveChanges();
            
            _movesHub.Clients.All.InvokeAsync("Update", BuildMoveHistory.GetHtml(game.Turns.Select(s => s.ToPdnTurn()).ToList()));
            _boardHub.Clients.All.InvokeAsync("Update", id, BuildBoard.GetHtml(move, true));
            _opponentsHub.Clients.All.InvokeAsync("Update", ((Player)game.CurrentPlayer).ToString(), move.GameStatus().ToString());

            return Content("");
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
            _boardHub.Clients.All.InvokeAsync("Update", id, BuildBoard.GetHtml(game.ToGame(), true));
            _opponentsHub.Clients.All.InvokeAsync("Update", ((Player)game.CurrentPlayer).ToString(), Status.InProgress.ToString());

            return Content("");
        }

        public ActionResult DisplayGame(Guid moveID)
        {
            var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.Turns.Any(a => a.Moves.Any(m => m.ID == moveID)));

            var move = game.Turns.SelectMany(t => t.Moves).First(f => f.ID == moveID);
            var turn = game.Turns.First(f => f.ID == move.TurnID);

            bool isLastTurn()
            {
                return game.Turns.OrderBy(a => a.MoveNumber).Last().MoveNumber == turn.MoveNumber &&
                    (turn.Moves.Count == 1 || move.Player == (int) Player.White);
            }

            var controller = GameController.FromPosition((Variant)game.Variant, move.ResultingFen);

            _opponentsHub.Clients.Client(Request.HttpContext.Connection.Id).InvokeAsync("Update", ((Player)game.CurrentPlayer).ToString(), Status.BlackWin.ToString());

            return Content(BuildBoard.GetHtml(controller, isLastTurn()));
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

                write($@"<input id=""{turn.BlackMove.ID}"" class=""toggle"" name=""move"" type=""radio"" value=""{turn.BlackMove.DisplayString}"" />");
                write($@"<label for=""{turn.BlackMove.ID}"" onclick=""displayGame('{turn.BlackMove.ID}')"">{turn.BlackMove.DisplayString}</label>");
                
                if (turn.WhiteMove != null)
                {
                    write($@"<input id=""{turn.WhiteMove.ID}"" class=""toggle"" name=""move"" type=""radio"" value=""{turn.WhiteMove.DisplayString}"" />");
                    write($@"<label for=""{turn.WhiteMove.ID}"" onclick=""displayGame('{turn.WhiteMove.ID}')"">{turn.WhiteMove.DisplayString}</label>");
                }
                write("</li>");
            }
            write("</ol>");

            return stringWriter.ToString();
        }
    }

    public class BuildBoard
    {
        public static string GetHtml(GameController game, bool allowMove)
        {
            var stringWriter = new StringWriter();
            Action<string> write = stringWriter.Write;
            
            int getAdjustedIndex(int value)
            {
                return game.CurrentPlayer == Player.Black ? 7 - value : value;
            }

            write($@"<div class=""board"" id=""{game.ID}"" player=""{game.CurrentPlayer}"">");
            write(@"<svg width=""100%"" height=""100%"" style=""background-color: #cccccc"" version=""1.1"" xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 50 50"">");

            for (var row = 0; row < 8; row++)
            {
                for (var col = 0; col < 8; col++)
                {
                    write($@"<image {(allowMove ? $@"onclick=""boardClick({getAdjustedIndex(row)}, {getAdjustedIndex(col)})""" : "")} y=""{getAdjustedIndex(row) * 12.5m}%"" x=""{getAdjustedIndex(col) * 12.5m}%"" width=""12.5%"" height=""12.5%"" xlink:href=""/images/SteelTheme/{(getAdjustedIndex(col) % 2 == getAdjustedIndex(row) % 2 ? "Light" : "Dark")}Steel.png"" />");

                    var piece = game.Board[row, col];

                    if (piece != null)
                    {
                        write($@"<svg {(allowMove ? $@"onclick=""pieceClick({getAdjustedIndex(row)}, {getAdjustedIndex(col)})""" : "")} y=""{getAdjustedIndex(row) * 12.5m}%"" x=""{getAdjustedIndex(col) * 12.5m}%"" width=""12.5%"" height=""12.5%"">");

                        write($@"<image id=""piece{getAdjustedIndex(row)}{getAdjustedIndex(col)}"" height=""100%"" width=""100%"" xlink:href=""/images/SteelTheme/{piece.Player}{piece.PieceType}.png"" />");
                        write(@"<rect class=""selected-piece-highlight"" height=""100%"" width=""100%"" style=""fill: none; stroke: goldenrod""></rect>");

                        write("</svg>");
                    }
                }
            }

            write("</svg>");
            write("</div>");

            return stringWriter.ToString();
        }
    }
}