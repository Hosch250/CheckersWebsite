using CheckersWebsite.Enums;
using CheckersWebsite.MediatR;
using CheckersWebsite.SignalR;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System;
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
            var clients = new List<IClientProxy>();
            if (request.ViewModel.BlackPlayerID != Guid.Empty)
            {
                clients.Add(_signalRHub.Clients.Client(_mediator.Send(new GetClientConnectionMessage(request.ViewModel.BlackPlayerID)).Result));
            }
            if (request.ViewModel.WhitePlayerID != Guid.Empty)
            {
                clients.Add(_signalRHub.Clients.Client(_mediator.Send(new GetClientConnectionMessage(request.ViewModel.WhitePlayerID)).Result));
            };

            string gameStatus = "";
            switch(request.ViewModel.GameStatus)
            {
                case Status.InProgress:
                    gameStatus = $"{request.ViewModel.CurrentPlayer}'s Turn";
                    break;
                case Status.Drawn:
                    gameStatus = "Game Drawn";
                    break;
                case Status.BlackWin:
                    gameStatus = "Black Won";
                    break;
                case Status.WhiteWin:
                    gameStatus = "White Won";
                    break;
                case Status.Aborted:
                    gameStatus = "Game Aborted";
                    break;
            }

            foreach (var client in clients)
            {
                client.SendAsync("SetAttribute", "undo", "disabled", "");
                client.SendAsync("RemoveClass", "new-game", "hide");
                client.SendAsync("AddClass", "resign", "hide");
                client.SendAsync("SetHtml", ".player-to-move", gameStatus);
            }

            return Task.CompletedTask;
        }
    }
}
