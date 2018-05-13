using CheckersWebsite.SignalR;
using Microsoft.AspNetCore.SignalR;
using System.Linq;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CheckersWebsite.Controllers;

namespace CheckersWebsite.Actions.MoveActions
{
    public class UpdateOpponentStateAction : INotificationHandler<OnMoveNotification>
    {
        private readonly IHubContext<GameHub> _signalRHub;
        private readonly IMediator _mediator;
        public UpdateOpponentStateAction(IHubContext<GameHub> signalRHub, IMediator mediator)
        {
            _signalRHub = signalRHub;
            _mediator = mediator;
        }

        public Task Handle(OnMoveNotification request, CancellationToken cancellationToken)
        {
            var lastMoveDate = _mediator.Send(new GetLastMoveDateMessage(request.ViewModel)).Result;
            _signalRHub.Clients
                .Group(request.ViewModel.ID.ToString())
                .InvokeAsync("UpdateOpponentState", request.ViewModel.ID, lastMoveDate, request.ViewModel.CurrentPlayer.ToString(), request.ViewModel.GameStatus.ToString());
            return Task.CompletedTask;
        }
    }
}