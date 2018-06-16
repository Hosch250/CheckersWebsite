using MediatR;
using CheckersWebsite.ViewModels;

namespace CheckersWebsite.Actions.GameConnectedActions
{
    public class OnGameConnectedNotification : INotification
    {
        public GameViewModel ViewModel { get; }

        public OnGameConnectedNotification(GameViewModel viewModel)
        {
            ViewModel = viewModel;
        }
    }
}