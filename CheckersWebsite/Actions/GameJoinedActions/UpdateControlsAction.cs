﻿using CheckersWebsite.MediatR;
using CheckersWebsite.SignalR;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CheckersWebsite.Actions.GameJoinedActions
{
    public class UpdateControlsAction : INotificationHandler<OnGameJoinedNotification>
    {
        private readonly IMediator _mediator;
        private readonly IHubContext<GameHub> _signalRHub;

        public UpdateControlsAction(IHubContext<GameHub> signalRHub, IMediator mediator)
        {
            _signalRHub = signalRHub;
            _mediator = mediator;
        }

        public Task Handle(OnGameJoinedNotification notification, CancellationToken cancellationToken)
        {
            _signalRHub.Clients.All.InvokeAsync("AddClass", "join", "hide");
            _signalRHub.Clients.Client(_mediator.Send(new GetClientConnectionMessage(notification.CurrentPlayerID)).Result).InvokeAsync("AddClass", notification.ViewModel.BlackPlayerID == notification.CurrentPlayerID ? "black-player-text" : "white-player-text", "bold");

            _signalRHub.Clients.Client(_mediator.Send(new GetClientConnectionMessage(notification.CurrentPlayerID)).Result).InvokeAsync("AddClass", "new-game", "hide");
            _signalRHub.Clients.Client(_mediator.Send(new GetClientConnectionMessage(notification.CurrentPlayerID)).Result).InvokeAsync("RemoveClass", "resign", "hide");

            var clients = new List<IClientProxy>
            {
                _signalRHub.Clients.Client(_mediator.Send(new GetClientConnectionMessage(notification.ViewModel.BlackPlayerID)).Result),
                _signalRHub.Clients.Client(_mediator.Send(new GetClientConnectionMessage(notification.ViewModel.WhitePlayerID)).Result)
            };

            foreach (var client in clients)
            {
                client.InvokeAsync("SetAttribute", "resign", "title", "Resign");
                client.InvokeAsync("SetHtml", "#resign .sr-only", "Resign");
            }

            return Task.CompletedTask;
        }
    }
}
