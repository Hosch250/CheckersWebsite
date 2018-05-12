using CheckersWebsite.SignalR;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using CheckersWebsite.Views.Controls;
using CheckersWebsite.Enums;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CheckersWebsite.Controllers;

namespace CheckersWebsite.Actions.MoveActions
{
    public class UpdateBoardAction : INotificationHandler<OnMoveNotification>
    {
        private readonly IHubContext<GameHub> _signalRHub;
        private readonly Database.Context _context;
        private readonly IMediator _mediator;
        public UpdateBoardAction(IHubContext<GameHub> signalRHub, Database.Context context, IMediator mediator)
        {
            _signalRHub = signalRHub;
            _context = context;
            _mediator = mediator;
        }

        private string GetClientConnection(Guid id)
        {
            return _context.Players.Find(id).ConnectionID;
        }

        Dictionary<string, object> GetViewData(Guid localPlayerID, Player orientation)
        {
            return new Dictionary<string, object>
            {
                ["playerID"] = localPlayerID,
                ["orientation"] = orientation
            };
        }

        public Task Handle(OnMoveNotification request, CancellationToken cancellationToken)
        {
            var lastMoveDate = _mediator.Send(new GetLastMoveDateMessage(request.ViewModel)).Result;

            if (request.ViewModel.BlackPlayerID != ComputerPlayer.ComputerPlayerID)
            {
                _signalRHub.Clients.Client(GetClientConnection(request.ViewModel.BlackPlayerID)).InvokeAsync("UpdateBoard", request.ViewModel.ID, lastMoveDate,
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(request.ViewModel.BlackPlayerID, Player.Black)),
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(request.ViewModel.BlackPlayerID, Player.White)));
            }

            if (request.ViewModel.WhitePlayerID != ComputerPlayer.ComputerPlayerID)
            {
                _signalRHub.Clients.Client(GetClientConnection(request.ViewModel.WhitePlayerID)).InvokeAsync("UpdateBoard", request.ViewModel.ID, lastMoveDate,
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(request.ViewModel.WhitePlayerID, Player.Black)),
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(request.ViewModel.WhitePlayerID, Player.White)));
            }

            _signalRHub.Clients
                .AllExcept(new List<string> { GetClientConnection(request.ViewModel.BlackPlayerID), GetClientConnection(request.ViewModel.WhitePlayerID) })
                .InvokeAsync("UpdateBoard", request.ViewModel.ID, lastMoveDate,
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(Guid.Empty, Player.Black)),
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(Guid.Empty, Player.White)));
            return Task.CompletedTask;
        }
    }
}