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

        public GameHub(Database.Context context)
        {
            _context = context;
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
            return base.OnConnectedAsync();
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
