using MediatR;
using CheckersWebsite.ViewModels;

namespace CheckersWebsite.Actions.MoveActions
{
    public class OnMoveNotification : INotification
    {
        public OnMoveNotification(GameViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public GameViewModel ViewModel { get; }
    }
}