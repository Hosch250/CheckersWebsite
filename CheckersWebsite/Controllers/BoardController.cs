using CheckersWebsite.Facade;
using Microsoft.AspNetCore.Mvc;
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

        // todo: figure out why the auto-converter can't handle `Piece[,]`
        public ActionResult MovePiece(string fen, Coord start, Coord end)
        {
            var controller = GameController.FromPosition(Variant.AmericanCheckers, fen);
            controller.ID = new Guid("25305799-AAE0-4B42-86CF-59CD2E464D3D");

            if (!controller.IsValidMove(start, end))
            {
                Response.StatusCode = 403;
                return null;
            }

            var move = controller.Move(start, end);
            var game = context.Games.FirstOrDefault();
            if (game == null)
            {
                context.Games.Add(move);
            }
            else
            {
                game = move;
            }

            context.SaveChanges();

            return PartialView("~/Views/Controls/CheckersBoard.cshtml", move);
        }
    }
}