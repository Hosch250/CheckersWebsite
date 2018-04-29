using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using CheckersWebsite.Models;
using Microsoft.EntityFrameworkCore;
using CheckersWebsite.Facade;
using System;
using Microsoft.AspNetCore.SignalR;
using CheckersWebsite.SignalR;
using System.Threading.Tasks;

namespace CheckersWebsite.Controllers
{
    public class HomeController : Controller
    {
        private static Random Random = new Random();

        private readonly Database.Context _context;
        private readonly IHubContext<SignalRHub> _signalRHub;
        private readonly ComputerPlayer _computerPlayer;

        public HomeController(Database.Context context,
            IHubContext<SignalRHub> signalRHub,
            ComputerPlayer computerPlayer)
        {
            _context = context;
            _signalRHub = signalRHub;
            _computerPlayer = computerPlayer;
        }

        private Guid? GetPlayerID()
        {
            if (Request.Cookies.Keys.All(a => a != "playerID"))
            {
                return null;
            }

            return Guid.Parse(Request.Cookies["playerID"]);
        }

        public IActionResult Index()
        {
            var playerID = GetPlayerID() ?? Guid.NewGuid();
            ViewData.Add("playerID", playerID);

            var games = _context.Games
                .OrderByDescending(g => g.CreatedOn)
                .ToList();
            
            return View(games);
        }

        public IActionResult BoardEditor()
        {
            return View();
        }

        public IActionResult Game(Guid id)
        {
            var playerID = GetPlayerID() ?? Guid.NewGuid();

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

            ViewData.Add("playerID", playerID);
            ViewData.Add("blackPlayerID", game.BlackPlayerID);
            ViewData.Add("whitePlayerID", game.WhitePlayerID);
            ViewData.Add("orientation", playerID == game.WhitePlayerID ? Player.White : Player.Black);

            Task.Run(() => _computerPlayer.DoComputerMove(game.ID));
            return View("~/Views/Controls/Game.cshtml", game.ToGame());
        }

        public ActionResult NewGame(Variant variant, Opponent blackOpponent, Opponent whiteOpponent, int blackStrength, int whiteStrength, string fen)
        {
            var playerID = GetPlayerID().Value;
            ViewData.Add("playerID", playerID);


            Database.Game newGame;
            if (!string.IsNullOrWhiteSpace(fen))
            {
                if (!GameController.TryFromPosition(variant, fen, out var game))
                {
                    Response.StatusCode = 403;
                    return Redirect("/");
                }
                else
                {
                    newGame = game.ToGame();
                }
            }
            else
            {
                newGame = GameController.FromVariant(variant).ToGame();
            }

            newGame.BlackPlayerStrength = -1;
            newGame.WhitePlayerStrength = -1;

            if (blackOpponent == Opponent.Human && whiteOpponent == Opponent.Human)
            {
                var player = Random.Next(0, 2);
                if (player == (int)Player.Black)
                {
                    newGame.BlackPlayerID = playerID;
                }
                else
                {
                    newGame.WhitePlayerID = playerID;
                }
            }
            else
            {
                if (blackOpponent == Opponent.Computer)
                {
                    newGame.BlackPlayerID = ComputerPlayer.ComputerPlayerID;
                    newGame.BlackPlayerStrength = blackStrength;
                }
                if (whiteOpponent == Opponent.Computer)
                {
                    newGame.WhitePlayerID = ComputerPlayer.ComputerPlayerID;
                    newGame.WhitePlayerStrength = whiteStrength;
                }
            }

            _context.Games.Add(newGame);
            _context.SaveChanges();
            
            return Redirect($"/Home/Game/{newGame.ID}");
        }

        public ActionResult Rules()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
