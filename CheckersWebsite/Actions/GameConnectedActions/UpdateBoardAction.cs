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
using System.Linq;

namespace CheckersWebsite.Actions.GameConnectedActions
{
    public class UpdateBoardAction : INotificationHandler<OnGameConnectedNotification>
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
            return _context.Players.FirstOrDefault(f => f.ID == id)?.ConnectionID;
        }

        Dictionary<string, object> GetViewData(Guid localPlayerID, Player orientation)
        {
            return new Dictionary<string, object>
            {
                ["playerID"] = localPlayerID,
                ["orientation"] = orientation
            };
        }

        public Task Handle(OnGameConnectedNotification request, CancellationToken cancellationToken)
        {
            var lastMoveDate = _mediator.Send(new GetLastMoveDateMessage(request.ViewModel)).Result;

            var blackConnection = GetClientConnection(request.ViewModel.BlackPlayerID);
            var whiteConnection = GetClientConnection(request.ViewModel.WhitePlayerID);

            if (request.ViewModel.BlackPlayerID != ComputerPlayer.ComputerPlayerID && blackConnection != null)
            {
                _signalRHub.Clients.Client(blackConnection).SendAsync("UpdateBoard", request.ViewModel.ID, lastMoveDate,
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(request.ViewModel.BlackPlayerID, Player.Black)),
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(request.ViewModel.BlackPlayerID, Player.White)));
            }

            if (request.ViewModel.WhitePlayerID != ComputerPlayer.ComputerPlayerID && whiteConnection != null)
            {
                _signalRHub.Clients.Client(whiteConnection).SendAsync("UpdateBoard", request.ViewModel.ID, lastMoveDate,
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(request.ViewModel.WhitePlayerID, Player.Black)),
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(request.ViewModel.WhitePlayerID, Player.White)));
            }

            _signalRHub.Clients
                .GroupExcept(request.ViewModel.ID.ToString(), blackConnection, whiteConnection)
                .SendAsync("UpdateBoard", request.ViewModel.ID, lastMoveDate,
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(Guid.Empty, Player.Black)),
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(Guid.Empty, Player.White)));

            return Task.CompletedTask;
        }
    }
}