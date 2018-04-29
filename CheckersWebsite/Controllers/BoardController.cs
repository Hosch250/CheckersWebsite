using CheckersWebsite.Facade;
using CheckersWebsite.SignalR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Project.Utilities;

namespace CheckersWebsite.Controllers
{
    public class BoardController : Controller
    {
        private readonly Database.Context _context;
        private readonly IHubContext<SignalRHub> _signalRHub;
        private readonly IViewRenderService _viewRenderService;
        private readonly ComputerPlayer _computerPlayer;

        public BoardController(Database.Context context,
            IHubContext<SignalRHub> signalRHub,
            IViewRenderService viewRenderService,
            ComputerPlayer computerPlayer)
        {
            _context = context;
            _signalRHub = signalRHub;
            _viewRenderService = viewRenderService;
            _computerPlayer = computerPlayer;
        }

        private Theme GetThemeOrDefault()
        {
            if (Request.Cookies.Keys.All(a => a != "theme"))
            {
                return Theme.Steel;
            }

            return Enum.Parse(typeof(Theme), Request.Cookies["theme"]) as Theme? ?? Theme.Steel;
        }

        private string GetClientConnection(Guid id)
        {
            return _context.Players.Find(id).ConnectionID;
        }

        public async Task<ActionResult> MovePiece(Guid id, Coord start, Coord end)
        {
            var playerID = GetPlayerID();
            if (!playerID.HasValue)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.ID == id);

            if (game == null ||
                game.GameStatus != (int)Status.InProgress ||
                (game.BlackPlayerID != playerID && game.CurrentPlayer == (int)Player.Black) ||
                (game.WhitePlayerID != playerID && game.CurrentPlayer == (int)Player.White))
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var controller = game.ToGame();

            if (!controller.IsValidMove(start, end))
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var move = controller.Move(start, end);
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
            }
            else
            {
                game.Turns.Add(move.MoveHistory.Last().ToPdnTurn());
                game.Fen = turn.Moves.Single().ResultingFen;
            }

            game.CurrentPosition = move.GetCurrentPosition();
            game.CurrentPlayer = (int)move.CurrentPlayer;
            game.GameStatus = (int)move.GetGameStatus();

            _context.SaveChanges();

            Dictionary<string, object> GetViewData(Guid localPlayerID, Player orientation)
            {
                return new Dictionary<string, object>
                {
                    ["playerID"] = localPlayerID,
                    ["blackPlayerID"] = game.BlackPlayerID,
                    ["whitePlayerID"] = game.WhitePlayerID,
                    ["orientation"] = orientation,
                    ["theme"] = GetThemeOrDefault(),
                    ["blackStrength"] = game.BlackPlayerStrength,
                    ["whiteStrength"] = game.WhitePlayerStrength
                };
            }

            var moveHistory = await _viewRenderService.RenderToStringAsync("Controls/MoveControl", move.MoveHistory, new Dictionary<string, object>());

            _signalRHub.Clients.Client(GetClientConnection(game.BlackPlayerID)).InvokeAsync("UpdateBoard", id,
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, GetViewData(game.BlackPlayerID, Player.Black)),
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, GetViewData(game.BlackPlayerID, Player.White)));

            _signalRHub.Clients.Client(GetClientConnection(game.WhitePlayerID)).InvokeAsync("UpdateBoard", id,
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, GetViewData(game.WhitePlayerID, Player.Black)),
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, GetViewData(game.WhitePlayerID, Player.White)));

            _signalRHub.Clients.AllExcept(new List<string> { GetClientConnection(game.BlackPlayerID), GetClientConnection(game.WhitePlayerID) }).InvokeAsync("UpdateBoard", id,
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, GetViewData(Guid.Empty, Player.Black)),
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, GetViewData(Guid.Empty, Player.White)));

            _signalRHub.Clients.All.InvokeAsync("UpdateMoves", moveHistory);
            _signalRHub.Clients.All.InvokeAsync("UpdateOpponentState", ((Player)game.CurrentPlayer).ToString(), move.GetGameStatus().ToString());

            if (game.Turns.Count == 1 && game.Turns.ElementAt(0).Moves.Count == 1)
            {
                _signalRHub.Clients.All.InvokeAsync("SetAttribute", "undo", "disabled", "");
            }
            else
            {
                _signalRHub.Clients.All.InvokeAsync("RemoveAttribute", "undo", "disabled");
            }

            _signalRHub.Clients.All.InvokeAsync(move.IsDrawn() || move.IsWon() ? "RemoveClass" : "AddClass", "new-game", "hide");
            _signalRHub.Clients.All.InvokeAsync(move.IsDrawn() || move.IsWon() ? "AddClass" : "RemoveClass", "resign", "hide");

            _computerPlayer.DoComputerMove(game.ID);
            return Content("");
        }

        public async Task<ActionResult> Undo(Guid id)
        {
            var playerID = GetPlayerID();
            if (!playerID.HasValue)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.ID == id);

            if (game == null ||
                game.GameStatus != (int)Status.InProgress ||
                game.BlackPlayerID != playerID && game.WhitePlayerID != playerID)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var lastTurn = game.Turns.OrderBy(a => a.MoveNumber).Last();

            if (lastTurn.Moves.Count == 2)
            {
                // todo: figure out which is the last move based on variant
                lastTurn.Moves.Remove(lastTurn.Moves.Single(s => (Player)s.Player == Player.White));
                game.Fen = game.Turns.Last().Moves.Single(s => (Player)s.Player == Player.Black).ResultingFen;
            }
            else
            {
                if (game.Turns.Count == 1)
                {
                    Response.StatusCode = 403;
                    return null;
                }

                game.Turns.Remove(lastTurn);
                game.Fen = game.Turns.Last().Moves.Single(s => (Player)s.Player == Player.White).ResultingFen;
            }

            game.CurrentPosition = -1;

            switch ((Player)game.CurrentPlayer)
            {
                case Player.White:
                    game.CurrentPlayer = (int)Player.Black;
                    break;
                case Player.Black:
                    game.CurrentPlayer = (int)Player.White;
                    break;
                default:
                    break;
            }

            _context.SaveChanges();

            var controller = game.ToGame();
            
            var moveHistory = await _viewRenderService.RenderToStringAsync("Controls/MoveControl", controller.MoveHistory, new Dictionary<string, object>());

            Dictionary<string, object> GetViewData(Guid localPlayerID, Player orientation)
            {
                return new Dictionary<string, object>
                {
                    ["playerID"] = localPlayerID,
                    ["blackPlayerID"] = game.BlackPlayerID,
                    ["whitePlayerID"] = game.WhitePlayerID,
                    ["orientation"] = orientation,
                    ["theme"] = GetThemeOrDefault(),
                    ["blackStrength"] = game.BlackPlayerStrength,
                    ["whiteStrength"] = game.WhitePlayerStrength
                };
            }
            
            _signalRHub.Clients.Client(GetClientConnection(game.BlackPlayerID)).InvokeAsync("UpdateBoard", id,
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", controller, GetViewData(game.BlackPlayerID, Player.Black)),
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", controller, GetViewData(game.BlackPlayerID, Player.White)));

            _signalRHub.Clients.Client(GetClientConnection(game.WhitePlayerID)).InvokeAsync("UpdateBoard", id,
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", controller, GetViewData(game.WhitePlayerID, Player.Black)),
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", controller, GetViewData(game.WhitePlayerID, Player.White)));

            _signalRHub.Clients.AllExcept(new List<string> { GetClientConnection(game.BlackPlayerID), GetClientConnection(game.WhitePlayerID) }).InvokeAsync("UpdateBoard", id,
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", controller, GetViewData(Guid.Empty, Player.Black)),
                await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", controller, GetViewData(Guid.Empty, Player.White)));

            _signalRHub.Clients.All.InvokeAsync("UpdateMoves", moveHistory);

            _signalRHub.Clients.All.InvokeAsync("UpdateOpponentState", ((Player)game.CurrentPlayer).ToString(), Status.InProgress.ToString());

            if (game.Turns.Count == 1 && game.Turns.ElementAt(0).Moves.Count == 1)
            {
                _signalRHub.Clients.All.InvokeAsync("SetAttribute", "undo", "disabled", "");
            }
            else
            {
                _signalRHub.Clients.All.InvokeAsync("RemoveAttribute", "undo", "disabled");
            }

            _computerPlayer.DoComputerMove(game.ID);
            return Content("");
        }

        public ActionResult Resign(Guid id)
        {
            var playerID = GetPlayerID();
            if (!playerID.HasValue)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var game = _context.Games.FirstOrDefault(f => f.ID == id);
            if (game == null ||
                game.GameStatus != (int) Status.InProgress ||
                game.BlackPlayerID != playerID && game.WhitePlayerID != playerID)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            if (game.BlackPlayerID == Guid.Empty || game.WhitePlayerID == Guid.Empty)
            {
                game.GameStatus = (int)Status.Aborted;
            }
            else
            {
                game.GameStatus = playerID == game.BlackPlayerID ? (int)Status.WhiteWin : (int)Status.BlackWin;
            }
            _context.SaveChanges();

            _signalRHub.Clients.All.InvokeAsync("UpdateOpponentState", ((Player)game.CurrentPlayer).ToString(), game.GameStatus.ToString());
            _signalRHub.Clients.All.InvokeAsync("RemoveClass", "new-game", "hide");
            _signalRHub.Clients.All.InvokeAsync("AddClass", "resign", "hide");

            return Content("");
        }

        public async Task<ActionResult> DisplayGame(Guid moveID, Player player)
        {
            var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.Turns.Any(a => a.Moves.Any(m => m.ID == moveID)));

            if (game == null)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var move = game.Turns.SelectMany(t => t.Moves).First(f => f.ID == moveID);
            var turn = game.Turns.First(f => f.ID == move.TurnID);

            var controller = GameController.FromPosition((Variant)game.Variant, move.ResultingFen);
            controller.ID = game.ID;

            var viewData = new Dictionary<string, object>
            {
                ["playerID"] = GetPlayerID(),
                ["blackPlayerID"] = game.BlackPlayerID,
                ["whitePlayerID"] = game.WhitePlayerID,
                ["orientation"] = player,
                ["theme"] = GetThemeOrDefault(),
                ["blackStrength"] = game.BlackPlayerStrength,
                ["whiteStrength"] = game.WhitePlayerStrength
            };

            var board = await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", controller, viewData);
            return Content(board);
        }

        public async Task<ActionResult> Join(Guid id)
        {
            var playerID = GetPlayerID();
            if (!playerID.HasValue)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var game = _context.Games.FirstOrDefault(f => f.ID == id);
            if (game.BlackPlayerID != Guid.Empty && game.WhitePlayerID != Guid.Empty ||
                game.BlackPlayerID == playerID ||
                game.WhitePlayerID == playerID)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            if (game.BlackPlayerID == Guid.Empty)
            {
                game.BlackPlayerID = playerID.Value;
            }
            else if (game.WhitePlayerID == Guid.Empty)
            {
                game.WhitePlayerID = playerID.Value;
            }

            _context.SaveChanges();
            
            _signalRHub.Clients.All.InvokeAsync("AddClass", "join", "hide");
            _signalRHub.Clients.Client(GetClientConnection(playerID.Value)).InvokeAsync("AddClass", game.BlackPlayerID == playerID.Value ? "black-player-text" : "white-player-text", "bold");

            _signalRHub.Clients.Client(GetClientConnection(playerID.Value)).InvokeAsync("AddClass", "new-game", "hide");
            _signalRHub.Clients.Client(GetClientConnection(playerID.Value)).InvokeAsync("RemoveClass", "resign", "hide");

            _signalRHub.Clients.All.InvokeAsync("SetAttribute", "resign", "title", "Resign");
            _signalRHub.Clients.All.InvokeAsync("SetHtml", "#resign .sr-only", "Resign");

            var viewData = new Dictionary<string, object>
            {
                ["playerID"] = playerID,
                ["blackPlayerID"] = game.BlackPlayerID,
                ["whitePlayerID"] = game.WhitePlayerID,
                ["orientation"] = game.BlackPlayerID == playerID ? Player.Black : Player.White,
                ["theme"] = GetThemeOrDefault(),
                ["blackStrength"] = game.BlackPlayerStrength,
                ["whiteStrength"] = game.WhitePlayerStrength
        };

            var board = await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", game.ToGame(), viewData);
            return Content(board);
        }

        public async Task<ActionResult> Orientate(Guid id, Guid? moveID, Player orientation)
        {
            var game = _context.Games
                    .Include("Turns")
                    .Include("Turns.Moves")
                    .FirstOrDefault(f => f.ID == id);

            if (game == null)
            {
                Response.StatusCode = 403;
                return Content("");
            }

            var move = game.Turns.SelectMany(t => t.Moves).FirstOrDefault(f => f.ID == moveID) ??
                game.Turns.OrderBy(o => o.MoveNumber).LastOrDefault()?.Moves.OrderBy(a => a.CreatedOn).LastOrDefault();

            var fen = move?.ResultingFen ?? game.Fen;

            var playerID = GetPlayerID();

            var controller = GameController.FromPosition((Variant)game.Variant, fen);
            controller.ID = game.ID;

            Dictionary<string, object>
                viewData = new Dictionary<string, object>
                {
                    ["playerID"] = playerID,
                    ["blackPlayerID"] = game.BlackPlayerID,
                    ["whitePlayerID"] = game.WhitePlayerID,
                    ["orientation"] = orientation,
                    ["theme"] = GetThemeOrDefault(),
                    ["blackStrength"] = game.BlackPlayerStrength,
                    ["whiteStrength"] = game.WhitePlayerStrength
                };

            var board = await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", game.ToGame(), viewData);
            return Content(board);
        }

        private Guid? GetPlayerID()
        {
            if (Request.Cookies.TryGetValue("playerID", out var id))
            {
                return Guid.Parse(id);
            }

            return null;
        }
    }
}

 
namespace Project.Utilities
{
    public interface IViewRenderService
    {
        Task<string> RenderToStringAsync(string viewName, object model, IDictionary<string, object> viewDataValues);
    }

    public class ViewRenderService : IViewRenderService
    {
        private readonly IRazorViewEngine _razorViewEngine;
        private readonly ITempDataProvider _tempDataProvider;
        private readonly IServiceProvider _serviceProvider;

        public ViewRenderService(IRazorViewEngine razorViewEngine,
            ITempDataProvider tempDataProvider,
            IServiceProvider serviceProvider)
        {
            _razorViewEngine = razorViewEngine;
            _tempDataProvider = tempDataProvider;
            _serviceProvider = serviceProvider;
        }

        public async Task<string> RenderToStringAsync(string viewName, object model, IDictionary<string, object> viewDataValues)
        {
            var httpContext = new DefaultHttpContext { RequestServices = _serviceProvider };
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            using (var sw = new StringWriter())
            {
                var viewResult = _razorViewEngine.FindView(actionContext, viewName, false);

                if (viewResult.View == null)
                {
                    throw new ArgumentNullException($"{viewName} does not match any available view");
                }

                var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
                {
                    Model = model
                };

                foreach (var kvp in viewDataValues)
                {
                    viewDictionary.Add(kvp);
                }

                var viewContext = new ViewContext(
                    actionContext,
                    viewResult.View,
                    viewDictionary,
                    new TempDataDictionary(actionContext.HttpContext, _tempDataProvider),
                    sw,
                    new HtmlHelperOptions()
                );

                await viewResult.View.RenderAsync(viewContext);
                return sw.ToString();
            }
        }
    }
}