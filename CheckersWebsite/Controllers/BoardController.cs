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
                    var newMove = turn.Moves.Single(s => !recordedTurn.Moves.Select(m => m.Player).Contains(s.Player));

                    recordedTurn.Moves.Add(newMove);
                }
                else
                {
                    game.Turns.Add(move.MoveHistory.Last().ToPdnTurn());
                }
            }

            context.SaveChanges();

            return PartialView("~/Views/Controls/CheckersBoard.cshtml", move);
        }
    }
}