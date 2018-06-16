using CheckersWebsite.SignalR;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using CheckersWebsite.Enums;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CheckersWebsite.MediatR;
using System.Linq;
using System;

namespace CheckersWebsite.Actions.GameConnectedActions
{
    public class UpdateControlsAction : INotificationHandler<OnGameConnectedNotification>
    {
        private readonly IHubContext<GameHub> _signalRHub;
        private readonly IMediator _mediator;
        public UpdateControlsAction(IHubContext<GameHub> signalRHub, IMediator mediator)
        {
            _signalRHub = signalRHub;
            _mediator = mediator;
        }

        public Task Handle(OnGameConnectedNotification request, CancellationToken cancellationToken)
        {
            var clients = new List<IClientProxy>();

            if (_mediator.Send(new GetClientConnectionMessage(request.ViewModel.BlackPlayerID)).Result is string blackID && blackID != null)
            {
                clients.Add(_signalRHub.Clients.Client(blackID));
            }

            if (_mediator.Send(new GetClientConnectionMessage(request.ViewModel.WhitePlayerID)).Result is string whiteID && whiteID != null)
            {
                clients.Add(_signalRHub.Clients.Client(whiteID));
            }


            foreach (var client in clients)
            {
                if (request.ViewModel.Turns.Count == 1 && (request.ViewModel.Turns[0].BlackMove == null || request.ViewModel.Turns[0].WhiteMove == null) || request.ViewModel.GameStatus != Status.InProgress)
                {
                    client.SendAsync("SetAttribute", "undo", "disabled", "");
                }
                else
                {
                    client.SendAsync("RemoveAttribute", "undo", "disabled");
                }
                client.SendAsync(request.ViewModel.GameStatus != Status.InProgress ? "RemoveClass" : "AddClass", "new-game", "hide");
                client.SendAsync(request.ViewModel.GameStatus != Status.InProgress ? "AddClass" : "RemoveClass", "resign", "hide");
            }

            return Task.CompletedTask;
        }
    }
}