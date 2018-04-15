using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using CheckersWebsite.Models;
using Microsoft.EntityFrameworkCore;
using CheckersWebsite.Facade;
using System;

namespace CheckersWebsite.Controllers
{
    public class HomeController : Controller
    {
        private readonly Database.Context _context;

        public HomeController(Database.Context context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var games = _context.Games
               .Include("Turns")
               .ToList();

            return View(games.Select(g => (ID: g.ID, Turns: g.Turns.Count)).ToList());
        }

        public IActionResult Game(Guid id)
        {
            var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.ID == id);

            return View("~/Views/Controls/Game.cshtml", game.ToGame());
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
