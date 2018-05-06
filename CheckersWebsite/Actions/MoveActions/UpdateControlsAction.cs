using CheckersWebsite.SignalR;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using CheckersWebsite.Enums;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CheckersWebsite.Controllers;

namespace CheckersWebsite.Actions.MoveActions
{
    public class UpdateControlsAction : INotificationHandler<OnMoveNotification>
    {
        private readonly IHubContext<SignalRHub> _signalRHub;
        private readonly IMediator _mediator;
        public UpdateControlsAction(IHubContext<SignalRHub> signalRHub, IMediator mediator)
        {
            _signalRHub = signalRHub;
            _mediator = mediator;
        }

        public Task Handle(OnMoveNotification request, CancellationToken cancellationToken)
        {
            var clients = new List<IClientProxy>
            {
                _signalRHub.Clients.Client(_mediator.Send(new GetClientConnectionMessage(request.ViewModel.BlackPlayerID)).Result),
                _signalRHub.Clients.Client(_mediator.Send(new GetClientConnectionMessage(request.ViewModel.WhitePlayerID)).Result)
            };

            foreach (var client in clients)
            {
                if (request.ViewModel.Turns.Count == 1 && (request.ViewModel.Turns[0].BlackMove == null || request.ViewModel.Turns[0].WhiteMove == null) || request.ViewModel.GameStatus != Status.InProgress)
                {
                    client.InvokeAsync("SetAttribute", "undo", "disabled", "");

                }
                else
                {
                    client.InvokeAsync("RemoveAttribute", "undo", "disabled");
                }
                client.InvokeAsync(request.ViewModel.GameStatus != Status.InProgress ? "RemoveClass" : "AddClass", "new-game", "hide");
                client.InvokeAsync(request.ViewModel.GameStatus != Status.InProgress ? "AddClass" : "RemoveClass", "resign", "hide");
            }

            return Task.CompletedTask;
        }
    }
}