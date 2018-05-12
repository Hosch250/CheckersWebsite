using CheckersWebsite.SignalR;
using Microsoft.AspNetCore.SignalR;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CheckersWebsite.Controllers;

namespace CheckersWebsite.Actions.MoveActions
{
    public class DoComputerMoveAction : INotificationHandler<OnMoveNotification>
    {
        private readonly ComputerPlayer _computerPlayer;
        private readonly IHubContext<GameHub> _signalRHub;
        public DoComputerMoveAction(IHubContext<GameHub> signalRHub, ComputerPlayer computerPlayer)
        {
            _signalRHub = signalRHub;
            _computerPlayer = computerPlayer;
        }

        public async Task Handle(OnMoveNotification request, CancellationToken cancellationToken)
        {
            await _computerPlayer.DoComputerMove(request.ViewModel.ID).ConfigureAwait(false);
        }
    }
}