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

            var gameStatus = Status.InProgress;
            if (move.IsDrawn())
            {
                gameStatus = Status.Drawn;
            }
            if (move.IsWon())
            {
                gameStatus = move.GetWinningPlayer() == Player.Black ? Status.BlackWin : Status.WhiteWin;
            }

            
            _movesHub.Clients.All.InvokeAsync("Update", BuildMoveHistory.GetHtml(game.Turns.Select(s => s.ToPdnTurn()).ToList()));
            _boardHub.Clients.All.InvokeAsync("Update", id, BuildBoard.GetHtml(move));
            _opponentsHub.Clients.All.InvokeAsync("Update", ((Player)game.CurrentPlayer).ToString(), gameStatus.ToString());

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
            var controller = game.ToGame();
            controller.ID = id;

            _movesHub.Clients.All.InvokeAsync("Update", BuildMoveHistory.GetHtml(game.Turns.Select(s => s.ToPdnTurn()).ToList()));
            _boardHub.Clients.All.InvokeAsync("Update", id, BuildBoard.GetHtml(controller));
            _opponentsHub.Clients.All.InvokeAsync("Update", ((Player)game.CurrentPlayer).ToString(), Status.InProgress.ToString());

            return Content("");
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

    public class BuildBoard
    {
        public static string GetHtml(GameController game)
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
                    write($@"<image onclick=""boardClick({getAdjustedIndex(row)}, {getAdjustedIndex(col)})"" y=""{getAdjustedIndex(row) * 12.5m}%"" x=""{getAdjustedIndex(col) * 12.5m}%"" width=""12.5%"" height=""12.5%"" xlink:href=""/images/SteelTheme/{(getAdjustedIndex(col) % 2 == getAdjustedIndex(row) % 2 ? "Light" : "Dark")}Steel.png"" />");

                    var piece = game.Board[row, col];

                    if (piece != null)
                    {
                        write($@"<svg onclick=""pieceClick({getAdjustedIndex(row)}, {getAdjustedIndex(col)})"" y=""{getAdjustedIndex(row) * 12.5m}%"" x=""{getAdjustedIndex(col) * 12.5m}%"" width=""12.5%"" height=""12.5%"">");

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