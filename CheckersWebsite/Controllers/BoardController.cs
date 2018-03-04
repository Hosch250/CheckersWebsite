using CheckersWebsite.Facade;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace CheckersWebsite.Controllers
{
    public class BoardController : Controller
    {
        // todo: figure out why the auto-converter can't handle `Piece[,]`
        public ActionResult MovePiece(string fen, Coord start, Coord end)
        {
            var controller = GameController.FromPosition(Variant.AmericanCheckers, fen);

            if (!controller.IsValidMove(start, end))
            {
                Response.StatusCode = 403;
                return null;
            }

            return PartialView("~/Views/CheckersBoard.cshtml", controller.Move(start, end));
        }
    }
}