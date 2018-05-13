using CheckersWebsite.ViewModels;
using MediatR;

namespace CheckersWebsite.Actions.GameCompletedActions
{
    public class OnGameCompletedNotification : INotification
    {
        public OnGameCompletedNotification(GameViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public GameViewModel ViewModel { get; }
    }
}
