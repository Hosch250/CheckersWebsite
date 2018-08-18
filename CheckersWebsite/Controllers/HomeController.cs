using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using CheckersWebsite.Models;
using Microsoft.EntityFrameworkCore;
using CheckersWebsite.Extensions;
using CheckersWebsite.Facade;
using System;
using Microsoft.AspNetCore.SignalR;
using CheckersWebsite.SignalR;
using CheckersWebsite.Enums;
using CheckersWebsite.ViewModels;
using CheckersWebsite.Actions.GameCreatedActions;
using MediatR;

namespace CheckersWebsite.Controllers
{
    public class HomeController : Controller
    {
        private static Random Random = new Random();

        private readonly Database.Context _context;
        private readonly IHubContext<GameHub> _signalRHub;
        private readonly ComputerPlayer _computerPlayer;
        private readonly IMediator _mediator;
        private readonly BoardController _boardController;

        public HomeController(Database.Context context,
            IHubContext<GameHub> signalRHub,
            ComputerPlayer computerPlayer,
            IMediator mediator,
            BoardController boardController)
        {
            _context = context;
            _signalRHub = signalRHub;
            _computerPlayer = computerPlayer;
            _mediator = mediator;
            this._boardController = boardController;
        }

        private Guid? GetPlayerID()
        {
            if (Request.Cookies.Keys.All(a => a != "playerID"))
            {
                return null;
            }

            return Guid.Parse(Request.Cookies["playerID"]);
        }

        private Theme GetThemeOrDefault()
        {
            if (Request.Cookies.Keys.All(a => a != "theme"))
            {
                return Theme.Steel;
            }

            return Enum.Parse(typeof(Theme), Request.Cookies["theme"]) as Theme? ?? Theme.Steel;
        }

        public IActionResult Index()
        {
            var playerID = GetPlayerID() ?? Guid.NewGuid();
            ViewData.Add("playerID", playerID);

            var games = _context.Games
                .OrderByDescending(g => g.CreatedOn)
                .Select(g => new GameDisplayViewModel
                {
                    ID = g.ID,
                    GameStatus = (Status)g.GameStatus,
                    Variant = (Variant)g.Variant,
                    BlackPlayerID = g.BlackPlayerID,
                    WhitePlayerID = g.WhitePlayerID
                })
                .ToList();
            
            return View(games);
        }

        public IActionResult BoardEditor()
        {
            ViewData.Add("theme", GetThemeOrDefault());
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
            ViewData.Add("theme", GetThemeOrDefault());
            ViewData.Add("orientation", playerID == game.WhitePlayerID ? Player.White : Player.Black);

            return View(game.ToGameViewModel());
        }

        public ActionResult NewGame(Variant variant, Opponent blackOpponent, Opponent whiteOpponent, int blackStrength, int whiteStrength, string fen, bool isBotGame = false)
        {
            if (blackOpponent == Opponent.Computer && whiteOpponent == Opponent.Computer)
            {
                Response.StatusCode = 403;
                return Redirect("/");
            }

            if (isBotGame)
            {
                var game = _context.Games.FirstOrDefault(f => f.IsBotGame && (f.WhitePlayerID == Guid.Empty || f.BlackPlayerID == Guid.Empty));
                if (game != null)
                {
                    return _boardController.Join(game.ID);
                }
            }

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

            newGame.RowVersion = DateTime.Now;

            newGame.BlackPlayerStrength = -1;
            newGame.WhitePlayerStrength = -1;

            newGame.IsBotGame = isBotGame;

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
                    newGame.WhitePlayerID = playerID;
                    newGame.BlackPlayerStrength = blackStrength;
                }
                if (whiteOpponent == Opponent.Computer)
                {
                    newGame.WhitePlayerID = ComputerPlayer.ComputerPlayerID;
                    newGame.BlackPlayerID = playerID;
                    newGame.WhitePlayerStrength = whiteStrength;
                }
            }

            _context.Games.Add(newGame);
            _context.SaveChanges();

            _mediator.Publish(new OnGameCreatedNotification(newGame.ToGameViewModel(), newGame.CurrentPlayer == (int)Player.Black ? newGame.BlackPlayerID : newGame.WhitePlayerID)).Wait();

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
