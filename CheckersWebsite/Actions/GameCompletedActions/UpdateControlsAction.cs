using CheckersWebsite.MediatR;
using CheckersWebsite.SignalR;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CheckersWebsite.Actions.GameCompletedActions
{
    public class UpdateControlsAction : INotificationHandler<OnGameCompletedNotification>
    {
        private readonly IHubContext<GameHub> _signalRHub;
        private readonly IMediator _mediator;
        public UpdateControlsAction(IHubContext<GameHub> signalRHub, IMediator mediator)
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
                client.SendAsync("SetAttribute", "undo", "disabled", "");
                client.SendAsync("RemoveClass", "new-game", "hide");
                client.SendAsync("AddClass", "resign", "hide");
            }

            return Task.CompletedTask;
        }
    }
}
