using System;
using MediatR;
using CheckersWebsite.ViewModels;

namespace CheckersWebsite.Controllers
{
    public class GetLastMoveDateMessage : IRequest<DateTime>
    {
        public GetLastMoveDateMessage(GameViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public GameViewModel ViewModel { get; }
    }
}