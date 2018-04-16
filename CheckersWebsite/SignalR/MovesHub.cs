﻿using CheckersWebsite.Facade;
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
        public Task Update(string player, string status)
        {
            return Clients.All.InvokeAsync("Update", player, status);
        }
    }
    public class BoardHub : Hub
    {
        public Task Update(string id, string blackBoard, string whiteBoard)
        {
            return Clients.All.InvokeAsync("Update", id, blackBoard, whiteBoard);
        }
    }
    public class ControlHub : Hub
    {
        public Task SetAttribute(string controlID, string attribute, string value)
        {
            return Clients.All.InvokeAsync("SetAttribute", controlID, attribute, value);
        }

        public Task RemoveAttribute(string controlID, string attribute)
        {
            return Clients.All.InvokeAsync("RemoveAttribute", controlID, attribute);
        }

        public Task AddClass(string controlID, string value)
        {
            return Clients.All.InvokeAsync("AddClass", controlID, value);
        }

        public Task RemoveClass(string controlID, string value)
        {
            return Clients.All.InvokeAsync("RemoveClass", controlID, value);
        }
    }
}
