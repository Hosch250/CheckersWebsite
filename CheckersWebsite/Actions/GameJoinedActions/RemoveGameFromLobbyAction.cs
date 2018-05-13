using CheckersWebsite.Controllers;
using CheckersWebsite.SignalR;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CheckersWebsite.Actions.GameJoinedActions
{
    public class RemoveGameFromLobbyAction : INotificationHandler<OnGameJoinedNotification>
    {
        private readonly IHubContext<GameHub> _signalRHub;

        public RemoveGameFromLobbyAction(IHubContext<GameHub> signalRHub)
        {
            _signalRHub = signalRHub;
        }

        public Task Handle(OnGameJoinedNotification notification, CancellationToken cancellationToken)
        {
            _signalRHub.Clients.Group("home").InvokeAsync("GameJoined", notification.ViewModel.ID);
            return Task.CompletedTask;
        }
    }
}
