using MediatR;
using CheckersWebsite.ViewModels;

namespace CheckersWebsite.Actions.MoveActions
{
    public class OnMoveNotification : INotification
    {
        public GameViewModel ViewModel { get; }

        public OnMoveNotification(GameViewModel viewModel)
        {
            ViewModel = viewModel;
        }
    }
}