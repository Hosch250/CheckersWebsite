using CheckersWebsite.SignalR;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using System.Threading;
using System.Threading.Tasks;

namespace CheckersWebsite.Actions.GameCreatedActions
{
    public class AddGameToLobbyAction : INotificationHandler<OnGameCreatedNotification>
    {
        private readonly IHubContext<GameHub> _signalRHub;

        public AddGameToLobbyAction(IHubContext<GameHub> signalRHub)
        {
            _signalRHub = signalRHub;
        }

        public Task Handle(OnGameCreatedNotification notification, CancellationToken cancellationToken)
        {
            var lobbyEntry =
$@"<tr>
    <td><a href=""/Home/Game/{notification.ViewModel.ID}"">{Resources.Resources.ResourceManager.GetString(notification.ViewModel.Variant.ToString())}</a></td>
    <td>{Resources.Resources.ResourceManager.GetString(notification.ViewModel.GameStatus.ToString())}</td>
</tr>";

            _signalRHub.Clients.Group("home").InvokeAsync("GameCreated", lobbyEntry);
            return Task.CompletedTask;
        }
    }
}
