using CheckersWebsite.ViewModels;
using MediatR;
using System;

namespace CheckersWebsite.Actions.GameCreatedActions
{
    public class OnGameCreatedNotification : INotification
    {
        public OnGameCreatedNotification(GameViewModel viewModel, Guid currentPlayerID)
        {
            ViewModel = viewModel;
        }

        public GameViewModel ViewModel { get; }
    }
}
