using CheckersWebsite.SignalR;
using Microsoft.AspNetCore.SignalR;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CheckersWebsite.Actions.MoveActions
{
    public class SignalGameStateAction : INotificationHandler<OnMoveNotification>
    {
        private readonly IHubContext<GameHub> _signalRHub;
        public SignalGameStateAction(IHubContext<GameHub> signalRHub)
        {
            _signalRHub = signalRHub;
        }

        public Task Handle(OnMoveNotification request, CancellationToken cancellationToken)
        {
            var data = JsonConvert.SerializeObject(request.ViewModel);
            _signalRHub.Clients.All.InvokeAsync("GameState", data);

            return Task.CompletedTask;
        }
    }
}