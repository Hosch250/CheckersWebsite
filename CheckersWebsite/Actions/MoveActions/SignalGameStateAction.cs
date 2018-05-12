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
        private readonly IHubContext<APIHub> _signalRHub;
        public SignalGameStateAction(IHubContext<APIHub> signalRHub)
        {
            _signalRHub = signalRHub;
        }

        public Task Handle(OnMoveNotification request, CancellationToken cancellationToken)
        {
            var data = JsonConvert.SerializeObject(request.ViewModel);
            _signalRHub.Clients.All.InvokeAsync("GameChanged", data);

            return Task.CompletedTask;
        }
    }
}