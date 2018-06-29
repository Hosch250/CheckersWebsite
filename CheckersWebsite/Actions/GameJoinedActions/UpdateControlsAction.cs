using CheckersWebsite.MediatR;
using CheckersWebsite.SignalR;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Linq;
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
            _signalRHub.Clients.All.SendAsync("AddClass", "join", "hide");

            var currentPlayerConnectionId = _mediator.Send(new GetClientConnectionMessage(notification.CurrentPlayerID)).Result;
            _signalRHub.Clients.Client(currentPlayerConnectionId).SendAsync("AddClass", notification.ViewModel.BlackPlayerID == notification.CurrentPlayerID ? "black-player-text" : "white-player-text", "bold");

            _signalRHub.Clients.Client(currentPlayerConnectionId).SendAsync("AddClass", "new-game", "hide");
            _signalRHub.Clients.Client(currentPlayerConnectionId).SendAsync("RemoveClass", "resign", "hide");
            _signalRHub.Clients.Client(currentPlayerConnectionId).SendAsync("SetHtml", "#chat .header", Resources.Resources.PlayerChat);

            var clientConnections = new List<string>
            {
                _mediator.Send(new GetClientConnectionMessage(notification.ViewModel.BlackPlayerID)).Result,
                _mediator.Send(new GetClientConnectionMessage(notification.ViewModel.WhitePlayerID)).Result
            };

            foreach (var connection in clientConnections.Where(c => !string.IsNullOrEmpty(c)))
            {
                var client = _signalRHub.Clients.Client(connection);
                client.SendAsync("SetAttribute", "resign", "title", "Resign");
                client.SendAsync("SetHtml", "#resign .sr-only", "Resign");
            }

            return Task.CompletedTask;
        }
    }
}
