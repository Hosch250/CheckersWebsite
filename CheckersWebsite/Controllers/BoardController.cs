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
        private readonly IHubContext<SignalRHub> _signalRHub;

        public BoardController(Database.Context context, IHubContext<SignalRHub> signalRHub)
        {
            _context = context;
            _signalRHub = signalRHub;
        }

        private Guid? GetPlayerID()
        {
            if (Request.Cookies.TryGetValue("playerID", out var id))
            {
                return Guid.Parse(id);
            }

            return null;
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
            
            var controller = game?.ToGame()
                ?? GameController.FromVariant(Variant.AmericanCheckers);

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

            _context.SaveChanges();

            var blackBoard = BuildBoard.GetHtml(move, Player.Black, true);
            var whiteBoard = BuildBoard.GetHtml(move, Player.White, true);

            _signalRHub.Clients.All.InvokeAsync("UpdateMoves", BuildMoveHistory.GetHtml(game.Turns.Select(s => s.ToPdnTurn()).ToList()));
            _signalRHub.Clients.All.InvokeAsync("UpdateBoard", id, blackBoard, whiteBoard);
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
                game.BlackPlayerID != playerID && game.WhitePlayerID != playerID)
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

            _context.SaveChanges();

            var controller = game.ToGame();
            var blackBoard = BuildBoard.GetHtml(controller, Player.Black, true);
            var whiteBoard = BuildBoard.GetHtml(controller, Player.White, true);
            
            _signalRHub.Clients.All.InvokeAsync("UpdateMoves", BuildMoveHistory.GetHtml(game.Turns.Select(s => s.ToPdnTurn()).ToList()));
            _signalRHub.Clients.All.InvokeAsync("UpdateBoard", id, blackBoard, whiteBoard);
            _signalRHub.Clients.All.InvokeAsync("UpdateOpponentState", ((Player)game.CurrentPlayer).ToString(), Status.InProgress.ToString());

            if (game.Turns.Count == 1 && game.Turns.ElementAt(0).Moves.Count == 1)
            {
                _signalRHub.Clients.All.InvokeAsync("SetAttribute", "undo", "disabled", "");
            }
            else
            {
                _signalRHub.Clients.All.InvokeAsync("RemoveAttribute", "undo", "disabled");
            }

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

            game.GameStatus = playerID == game.BlackPlayerID ? (int)Status.WhiteWin : (int)Status.BlackWin;
            _context.SaveChanges();

            _signalRHub.Clients.All.InvokeAsync("UpdateOpponentState", ((Player)game.CurrentPlayer).ToString(), game.GameStatus.ToString());
            _signalRHub.Clients.All.InvokeAsync("RemoveClass", "new-game", "hide");
            _signalRHub.Clients.All.InvokeAsync("AddClass", "resign", "hide");

            return Content("");
        }

        public ActionResult DisplayGame(Guid moveID, Player player)
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
            var turn = game.Turns.First(f => f.ID == move.TurnID);

            bool isLastTurn()
            {
                return game.Turns.OrderBy(a => a.MoveNumber).Last().MoveNumber == turn.MoveNumber &&
                    (turn.Moves.Count == 1 || move.Player == (int) Player.White);
            }

            var controller = GameController.FromPosition((Variant)game.Variant, move.ResultingFen);
            controller.ID = game.ID;
            return Content(BuildBoard.GetHtml(controller, player, isLastTurn()));
        }

        public ActionResult Join(Guid id, string connectionID)
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

            _context.SaveChanges();

            _signalRHub.Clients.All.InvokeAsync("AddClass", "join", "hide");
            _signalRHub.Clients.Client(connectionID).InvokeAsync("AddClass", game.BlackPlayerID == playerID ? "black-player-text" : "white-player-text", "bold");

            return Content(BuildBoard.GetHtml(game.ToGame(), game.BlackPlayerID == playerID.Value ? Player.Black : Player.White, true));
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

            var fen = move?.ResultingFen ?? game.Fen;

            var playerID = GetPlayerID();

            var controller = GameController.FromPosition((Variant)game.Variant, fen);
            controller.ID = game.ID;
            return Content(BuildBoard.GetHtml(controller, orientation, playerID.HasValue && (playerID.Value == game.BlackPlayerID || playerID.Value == game.WhitePlayerID) && fen == game.Fen));
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
        public static string GetHtml(GameController game, Player player, bool allowMove)
        {
            var stringWriter = new StringWriter();
            Action<string> write = stringWriter.Write;
            
            int getAdjustedIndex(int value)
            {
                return player == Player.Black ? 7 - value : value;
            }

            write($@"<div class=""board"" id=""{game.ID}"" orientation=""{player}"">");
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