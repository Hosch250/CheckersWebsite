using CheckersWebsite.Facade;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace CheckersWebsite.Controllers
{
    public class BoardController : Controller
    {
        private readonly Database.Context context;

        public BoardController(Database.Context context)
        {
            this.context = context;
        }

        public ActionResult MovePiece(Guid id, Coord start, Coord end)
        {
            var game = context.Games
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
                context.Games.Add(move.ToGame());
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
                    game.CurrentPosition = move.GetCurrentPosition();
                }
                else
                {
                    game.Turns.Add(move.MoveHistory.Last().ToPdnTurn());

                    game.Fen = turn.Moves.Single().ResultingFen;
                    game.CurrentPosition = move.GetCurrentPosition();
                }
            }

            context.SaveChanges();

            return PartialView("~/Views/Controls/CheckersBoard.cshtml", move);
        }
    }
}