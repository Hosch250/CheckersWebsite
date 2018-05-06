using CheckersWebsite.Controllers;
using CheckersWebsite.Enums;
using CheckersWebsite.SignalR;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CheckersWebsite.Actions.GameCompletedActions
{
    public class UpdateControlsAction : INotificationHandler<OnGameCompletedNotification>
    {
        private readonly IHubContext<SignalRHub> _signalRHub;
        private readonly IMediator _mediator;
        public UpdateControlsAction(IHubContext<SignalRHub> signalRHub, IMediator mediator)
        {
            _signalRHub = signalRHub;
            _mediator = mediator;
        }

        public Task Handle(OnGameCompletedNotification request, CancellationToken cancellationToken)
        {
            var clients = new List<IClientProxy>
            {
                _signalRHub.Clients.Client(_mediator.Send(new GetClientConnectionMessage(request.ViewModel.BlackPlayerID)).Result),
                _signalRHub.Clients.Client(_mediator.Send(new GetClientConnectionMessage(request.ViewModel.WhitePlayerID)).Result)
            };

            foreach (var client in clients)
            {
                client.InvokeAsync("SetAttribute", "undo", "disabled", "");
                client.InvokeAsync("RemoveClass", "new-game", "hide");
                client.InvokeAsync("AddClass", "resign", "hide");
            }

            return Task.CompletedTask;
        }
    }
}
