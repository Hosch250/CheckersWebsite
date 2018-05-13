using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;

namespace CheckersWebsite.SignalR
{
    public class GameHub : Hub
    {
        private readonly Database.Context _context;
        private readonly IHttpContextAccessor _contextAccessor;

        public GameHub(Database.Context context, IHttpContextAccessor contextAccessor)
        {
            _context = context;
            _contextAccessor = contextAccessor;
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
            return Task.CompletedTask;
        }

        public Task<Guid> GetNewPlayerID()
        {
            return Task.FromResult(Guid.NewGuid());
        }

        public override Task OnConnectedAsync()
        {
            var parameter = _contextAccessor.HttpContext.Request.Query["currentPage"].ToString();
            var group = parameter.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (parameter == "/" || parameter.ToLower() == "home")
            {
                Groups.AddAsync(Context.ConnectionId, "home");
            }
            else if (group.Length == 3 && group[0].ToLower() == "home" && group[1].ToLower() == "game")
            {
                Groups.AddAsync(Context.ConnectionId, group[2]);
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
