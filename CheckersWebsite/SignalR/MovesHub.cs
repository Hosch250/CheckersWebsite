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
        public Task Update(string player)
        {
            return Clients.All.InvokeAsync("Update", player);
        }
    }
}
