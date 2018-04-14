using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace CheckersWebsite.SignalR
{
    public class MovesHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            await Clients.All.InvokeAsync("SendAction", Context.User.Identity.Name, "joined");
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            await Clients.All.InvokeAsync("SendAction", Context.User.Identity.Name, "left");
        }

        public Task Update(string html)
        {
            return Clients.All.InvokeAsync("Update", html);
        }
    }
}
