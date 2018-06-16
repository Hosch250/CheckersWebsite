﻿using CheckersWebsite.SignalR;
using Microsoft.AspNetCore.SignalR;
using CheckersWebsite.Views.Controls;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CheckersWebsite.Controllers;

namespace CheckersWebsite.Actions.GameConnectedActions
{
    public class UpdateMoveHistoryAction : INotificationHandler<OnGameConnectedNotification>
    {
        private readonly IHubContext<GameHub> _signalRHub;
        private readonly IMediator _mediator;
        public UpdateMoveHistoryAction(IHubContext<GameHub> signalRHub, IMediator mediator)
        {
            _signalRHub = signalRHub;
            _mediator = mediator;
        }

        public Task Handle(OnGameConnectedNotification request, CancellationToken cancellationToken)
        {
            var lastMoveDate = _mediator.Send(new GetLastMoveDateMessage(request.ViewModel)).Result;

            _signalRHub.Clients
                .Group(request.ViewModel.ID.ToString())
                .SendAsync("UpdateMoves", request.ViewModel.ID, lastMoveDate, ComponentGenerator.GetMoveControl(request.ViewModel.Turns));
            return Task.CompletedTask;
        }
    }
}