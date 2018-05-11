using CheckersWebsite.SignalR;
using Microsoft.AspNetCore.SignalR;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CheckersWebsite.Controllers;

namespace CheckersWebsite.Actions.GameCreatedActions
{
    public class DoComputerMoveAction : INotificationHandler<OnGameCreatedNotification>
    {
        private readonly ComputerPlayer _computerPlayer;
        private readonly IHubContext<SignalRHub> _signalRHub;
        public DoComputerMoveAction(IHubContext<SignalRHub> signalRHub, ComputerPlayer computerPlayer)
        {
            _signalRHub = signalRHub;
            _computerPlayer = computerPlayer;
        }

        public async Task Handle(OnGameCreatedNotification request, CancellationToken cancellationToken)
        {
            await _computerPlayer.DoComputerMove(request.ViewModel.ID).ConfigureAwait(false);
        }
    }
}