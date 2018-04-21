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

        public BoardController(Database.Context context,
            IHubContext<SignalRHub> signalRHub,
            IViewRenderService viewRenderService)
        {
            _context = context;
            _signalRHub = signalRHub;
            _viewRenderService = viewRenderService;
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
            
            var blackViewData = new Dictionary<string, object>
            {
                ["playerID"] = game.BlackPlayerID,
                ["blackPlayerID"] = game.BlackPlayerID,
                ["whitePlayerID"] = game.WhitePlayerID
            };

            var whiteViewData = new Dictionary<string, object>
            {
                ["playerID"] = game.WhitePlayerID,
                ["blackPlayerID"] = game.BlackPlayerID,
                ["whitePlayerID"] = game.WhitePlayerID
            };

            var blackBoard = await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, blackViewData);
            var whiteBoard = await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", move, whiteViewData);

            var moveHistory = await _viewRenderService.RenderToStringAsync("Controls/MoveControl", move.MoveHistory, new Dictionary<string, object>());

            _signalRHub.Clients.All.InvokeAsync("UpdateBoard", id, blackBoard, whiteBoard);
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

            var blackViewData = new Dictionary<string, object>
            {
                ["playerID"] = game.BlackPlayerID,
                ["blackPlayerID"] = game.BlackPlayerID,
                ["whitePlayerID"] = game.WhitePlayerID
            };

            var whiteViewData = new Dictionary<string, object>
            {
                ["playerID"] = game.WhitePlayerID,
                ["blackPlayerID"] = game.BlackPlayerID,
                ["whitePlayerID"] = game.WhitePlayerID
            };

            var blackBoard = await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", controller, blackViewData);
            var whiteBoard = await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", controller, whiteViewData);

            var moveHistory = await _viewRenderService.RenderToStringAsync("Controls/MoveControl", controller.MoveHistory, new Dictionary<string, object>());
            
            _signalRHub.Clients.All.InvokeAsync("UpdateBoard", id, blackBoard, whiteBoard);
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

            bool isLastTurn()
            {
                return game.Turns.OrderBy(a => a.MoveNumber).Last().MoveNumber == turn.MoveNumber &&
                    (turn.Moves.Count == 1 || move.Player == (int) Player.White);
            }

            var controller = GameController.FromPosition((Variant)game.Variant, move.ResultingFen);
            controller.ID = game.ID;

            Dictionary<string, object> viewData;
            if (player == Player.Black)
            {
                viewData = new Dictionary<string, object>
                {
                    ["playerID"] = game.BlackPlayerID,
                    ["blackPlayerID"] = game.BlackPlayerID,
                    ["whitePlayerID"] = game.WhitePlayerID
                };
            }
            else
            {
                viewData = new Dictionary<string, object>
                {
                    ["playerID"] = game.WhitePlayerID,
                    ["blackPlayerID"] = game.BlackPlayerID,
                    ["whitePlayerID"] = game.WhitePlayerID
                };
            }

            var board = await _viewRenderService.RenderToStringAsync("Controls/CheckersBoard", controller, viewData);
            return Content(board);
        }

        public async Task<ActionResult> Join(Guid id, string connectionID)
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
            _signalRHub.Clients.Client(connectionID).InvokeAsync("AddClass", game.BlackPlayerID == playerID ? "black-player-text" : "white-player-text", "bold");

            _signalRHub.Clients.Client(connectionID).InvokeAsync("AddClass", "new-game", "hide");
            _signalRHub.Clients.Client(connectionID).InvokeAsync("RemoveClass", "resign", "hide");

            _signalRHub.Clients.All.InvokeAsync("SetAttribute", "resign", "title", "Resign");
            _signalRHub.Clients.All.InvokeAsync("SetHtml", "#resign .sr-only", "Resign");

            Dictionary<string, object> viewData;
            if (game.BlackPlayerID == playerID.Value)
            {
                viewData = new Dictionary<string, object>
                {
                    ["playerID"] = game.BlackPlayerID,
                    ["blackPlayerID"] = game.BlackPlayerID,
                    ["whitePlayerID"] = game.WhitePlayerID
                };
            }
            else
            {
                viewData = new Dictionary<string, object>
                {
                    ["playerID"] = game.WhitePlayerID,
                    ["blackPlayerID"] = game.BlackPlayerID,
                    ["whitePlayerID"] = game.WhitePlayerID
                };
            }

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

            Dictionary<string, object> viewData;
            if (orientation == Player.Black)
            {
                viewData = new Dictionary<string, object>
                {
                    ["playerID"] = game.BlackPlayerID,
                    ["blackPlayerID"] = game.BlackPlayerID,
                    ["whitePlayerID"] = game.WhitePlayerID
                };
            }
            else
            {
                viewData = new Dictionary<string, object>
                {
                    ["playerID"] = game.WhitePlayerID,
                    ["blackPlayerID"] = game.BlackPlayerID,
                    ["whitePlayerID"] = game.WhitePlayerID
                };
            }

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