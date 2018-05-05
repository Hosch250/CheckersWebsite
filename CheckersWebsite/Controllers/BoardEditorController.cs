using CheckersWebsite.Enums;
using CheckersWebsite.Facade;
using CheckersWebsite.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CheckersWebsite.Controllers
{
    public class BoardEditorController : Controller
    {
        private Theme GetThemeOrDefault()
        {
            if (Request.Cookies.Keys.All(a => a != "theme"))
            {
                return Theme.Steel;
            }

            return Enum.Parse(typeof(Theme), Request.Cookies["theme"]) as Theme? ?? Theme.Steel;
        }

        public IActionResult GetBoard(Variant variant, BoardEditorPosition position)
        {
            ViewData.Add("theme", GetThemeOrDefault());

            var board = position == BoardEditorPosition.Empty ? Board.EmptyBoard() : Board.DefaultBoard(variant);
            return PartialView("~/Views/Controls/CheckersBoardEditor.cshtml", board);
        }

        public IActionResult GetFEN(Variant variant, Player startingPlayer, List<PieceValue> pieces)
        {
            var numberMap = new Dictionary<char, int>
            {
                ['0'] = 0,
                ['1'] = 1,
                ['2'] = 2,
                ['3'] = 3,
                ['4'] = 4,
                ['5'] = 5,
                ['6'] = 6,
                ['7'] = 7,
                ['8'] = 8,
                ['9'] = 9
            };

            pieces = pieces.Select(s => {
                var coord = s.ID.Replace("piece", "");
                return new PieceValue
                    {
                        Coord = new Coord
                        {
                            Row = numberMap[coord[0]],
                            Column = numberMap[coord[1]]
                        },
                        Piece = s.Piece,
                        Player = s.Player
                    };
                }).ToList();

            var board = Board.EmptyBoard();
            pieces.ForEach(f =>
            {
                board.GameBoard[f.Coord.Row, f.Coord.Column] = new Piece(f.Player, f.Piece);
            });

            var controller = new GameController(variant, board, startingPlayer);
            return Content(controller.Fen);
        }
    }
}