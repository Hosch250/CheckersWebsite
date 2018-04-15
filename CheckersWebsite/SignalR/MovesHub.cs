using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CheckersWebsite.SignalR
{
    public class MovesHub : Hub
    {
        public Task Update(string html)
        {
            return Clients.All.InvokeAsync("Update", html);
        }
    }
    public class OpponentsHub : Hub
    {
        public Task Update(string player, bool isWon)
        {
            return Clients.All.InvokeAsync("Update", player);
        }
    }
    public class BoardHub : Hub
    {
        public Task Update(string id, string html)
        {
            return Clients.All.InvokeAsync("Update", id, html);
        }
    }
}
