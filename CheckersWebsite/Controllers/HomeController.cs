using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using CheckersWebsite.Models;
using Microsoft.EntityFrameworkCore;
using CheckersWebsite.Facade;
using System;
using System.Collections.Generic;

namespace CheckersWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly Database.Context _context;

        public HomeController(Database.Context context)
        {
            _context = context;
        }

        private Guid CreateCookieIfNotExists()
        {
            if (Request.Cookies.Keys.All(a => a != "playerID"))
            {
                var guid = Guid.NewGuid();
                Response.Cookies.Append(
                    "playerID",
                    guid.ToString(),
                    new Microsoft.AspNetCore.Http.CookieOptions()
                    {
                        Path = "/",
                        Expires = DateTime.Now.AddYears(1)
                    }
                );

                return guid;
            }

            return Guid.Parse(Request.Cookies["playerID"]);
        }

        public IActionResult Index()
        {
            var playerID = CreateCookieIfNotExists();
            ViewData.Add(new KeyValuePair<string, object>("playerID", playerID));

            var games = _context.Games
                .OrderByDescending(g => g.CreatedOn)
                .Include("Turns")
                .ToList();
            
            return View(games.Select(g =>
                (
                    ID: g.ID,
                    Turns: g.Turns.Count,
                    Status: Resources.Resources.ResourceManager.GetString(((Status)g.GameStatus).ToString())
                )).ToList());
        }

        public IActionResult Game(Guid id)
        {
            var playerID = CreateCookieIfNotExists();
            ViewData.Add(new KeyValuePair<string, object>("playerID", playerID));

            if (id == Guid.Empty)
            {
                Response.StatusCode = 404;
                return Content("");
            }

            var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.ID == id);

            if (game == null)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            ViewData.Add(new KeyValuePair<string, object>("blackPlayerID", game.BlackPlayerID));
            ViewData.Add(new KeyValuePair<string, object>("whitePlayerID", game.WhitePlayerID));

            return View("~/Views/Controls/Game.cshtml", game.ToGame());
        }

        public ActionResult NewGame()
        {
            var playerID = CreateCookieIfNotExists();
            ViewData.Add(new KeyValuePair<string, object>("playerID", playerID));

            var player = new Random().Next(0, 2);
            var newGame = GameController.FromVariant(Variant.AmericanCheckers).ToGame();

            if (player == (int)Player.Black)
            {
                newGame.BlackPlayerID = playerID;
            }
            else
            {
                newGame.WhitePlayerID = playerID;
            }

            _context.Games.Add(newGame);
            _context.SaveChanges();
            
            return Redirect($"/Home/Game/{newGame.ID}");
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
