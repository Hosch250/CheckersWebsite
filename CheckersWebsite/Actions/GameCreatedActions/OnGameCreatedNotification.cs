using CheckersWebsite.ViewModels;
using MediatR;
using System;

namespace CheckersWebsite.Actions.GameCreatedActions
{
    public class OnGameCreatedNotification : INotification
    {
        public GameViewModel ViewModel { get; }

        public OnGameCreatedNotification(GameViewModel viewModel, Guid currentPlayerID)
        {
            ViewModel = viewModel;
        }
    }
}
