using CheckersWebsite.ViewModels;
using MediatR;

namespace CheckersWebsite.Actions.GameCompletedActions
{
    public class OnGameCompletedNotification : INotification
    {
        public GameViewModel ViewModel { get; }

        public OnGameCompletedNotification(GameViewModel viewModel)
        {
            ViewModel = viewModel;
        }
    }
}
