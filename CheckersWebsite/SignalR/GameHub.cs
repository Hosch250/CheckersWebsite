using System;
using System.Linq;
using System.Threading.Tasks;
using CheckersWebsite.Actions.GameConnectedActions;
using CheckersWebsite.Controllers;
using CheckersWebsite.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CheckersWebsite.SignalR
{
    public class GameHub : Hub
    {
        private readonly Database.Context _context;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly IMediator _mediator;

        public GameHub(Database.Context context, IHttpContextAccessor contextAccessor, IMediator mediator)
        {
            _context = context;
            _contextAccessor = contextAccessor;
            _mediator = mediator;
        }
        
        private string GetClientConnection(Guid id)
        {
            return _context.Players.FirstOrDefault(f => f.ID == id)?.ConnectionID;
        }

        private Guid? GetPlayerID()
        {
            if (_contextAccessor.HttpContext.Request.Cookies.TryGetValue("playerID", out var id))
            {
                return Guid.Parse(id);
            }

            return null;
        }

        public Task MapPlayerConnection(Guid playerID)
        {
            var player = _context.Players.Find(playerID);
            if (player == null)
            {
                _context.Players.Add(new Database.Player
                {
                    ID = playerID,
                    ConnectionID = Context.ConnectionId
                });
            }
            else
            {
                player.ConnectionID = Context.ConnectionId;
                _context.Players.Update(player);
            }
            _context.SaveChanges();

            var parameter = _contextAccessor.HttpContext.Request.Query["currentPage"].ToString();
            var parts = parameter.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 3 && parts[0].ToLower() == "home" && parts[1].ToLower() == "game")
            {
                var gameID = Guid.Parse(parts[2]);
                var game = _context.Games
                        .Include("Turns")
                        .Include("Turns.Moves")
                        .FirstOrDefault(f => f.ID == gameID);
                var viewModel = game.ToGameViewModel();

                _mediator.Publish(new OnGameConnectedNotification(viewModel));
            }

            return Task.CompletedTask;
        }

        public Task<Guid> GetNewPlayerID()
        {
            return Task.FromResult(Guid.NewGuid());
        }
        
        public void NewMessage(Guid gameID, string message)
        {
            var game = _context.Games.FirstOrDefault(f => f.ID == gameID);
            var playerID = GetPlayerID();

            if (game == null || playerID == null)
            {
                return;
            }

            var blackConnection = GetClientConnection(game.BlackPlayerID);
            var whiteConnection = GetClientConnection(game.WhitePlayerID);

            if (Context.ConnectionId == blackConnection || Context.ConnectionId == whiteConnection)
            {
                var player = blackConnection == Context.ConnectionId ? Resources.Resources.Black : Resources.Resources.White;

                if (game.BlackPlayerID != ComputerPlayer.ComputerPlayerID && blackConnection != null)
                {
                    Clients.Client(blackConnection).SendAsync("ReceiveChatMessage", player + Resources.Resources.Player, message);
                }

                if (game.WhitePlayerID != ComputerPlayer.ComputerPlayerID && whiteConnection != null)
                {
                    Clients.Client(whiteConnection).SendAsync("ReceiveChatMessage", player + Resources.Resources.Player, message);
                }
            }
            else
            {
                Clients.GroupExcept(game.ID.ToString(), blackConnection, whiteConnection).SendAsync("ReceiveChatMessage", "Spectator", message);
            }
        }

        public override Task OnConnectedAsync()
        {
            var parameter = _contextAccessor.HttpContext.Request.Query["currentPage"].ToString();
            var group = parameter.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parameter == "/" || parameter.ToLower() == "home")
            {
                Groups.AddToGroupAsync(Context.ConnectionId, "home");
            }
            else if (group.Length == 3 && group[0].ToLower() == "home" && group[1].ToLower() == "game")
            {
                Groups.AddToGroupAsync(Context.ConnectionId, group[2]);
            }

            return Task.CompletedTask;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var player = _context.Players.FirstOrDefault(f => f.ConnectionID == Context.ConnectionId);

            if (player != null)
            {
                _context.Players.Remove(player);
                _context.SaveChanges();
            }

            return Task.CompletedTask;
        }
    }
}
