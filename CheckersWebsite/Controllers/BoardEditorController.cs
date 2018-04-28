using CheckersWebsite.Facade;
using Microsoft.AspNetCore.Mvc;

namespace CheckersWebsite.Controllers
{
    public class BoardEditorController : Controller
    {
        public IActionResult GetBoard(Variant variant, BoardEditorPosition position)
        {
            var board = position == BoardEditorPosition.Empty ? Board.EmptyBoard() : Board.DefaultBoard(variant);
            return PartialView("~/Views/Controls/CheckersBoardEditor.cshtml", board);
        }
    }
}