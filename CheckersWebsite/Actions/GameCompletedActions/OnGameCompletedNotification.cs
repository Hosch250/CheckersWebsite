using CheckersWebsite.ViewModels;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
