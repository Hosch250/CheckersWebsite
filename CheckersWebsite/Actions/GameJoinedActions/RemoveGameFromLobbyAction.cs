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
        private readonly IHubContext<SignalRHub> _signalRHub;

        public RemoveGameFromLobbyAction(IHubContext<SignalRHub> signalRHub)
        {
            _signalRHub = signalRHub;
        }

        public Task Handle(OnGameJoinedNotification notification, CancellationToken cancellationToken)
        {
            _signalRHub.Clients.All.InvokeAsync("GameJoined", notification.ViewModel.ID);
            return Task.CompletedTask;
        }
    }
}
