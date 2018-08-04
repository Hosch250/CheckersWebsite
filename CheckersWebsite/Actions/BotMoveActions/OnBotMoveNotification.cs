using MediatR;
using CheckersWebsite.ViewModels;

namespace CheckersWebsite.Actions.MoveActions
{
    public class OnBotMoveNotification : INotification
    {
        public GameViewModel ViewModel { get; }

        public OnBotMoveNotification(GameViewModel viewModel)
        {
            ViewModel = viewModel;
        }
    }
}