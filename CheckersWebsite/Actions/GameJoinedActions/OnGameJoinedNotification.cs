using CheckersWebsite.ViewModels;
using MediatR;
using System;

namespace CheckersWebsite.Actions.GameJoinedActions
{
    public class OnGameJoinedNotification : INotification
    {
        public GameViewModel ViewModel { get; }
        public Guid CurrentPlayerID { get; }

        public OnGameJoinedNotification(GameViewModel viewModel, Guid currentPlayerID)
        {
            ViewModel = viewModel;
            CurrentPlayerID = currentPlayerID;
        }
    }
}
