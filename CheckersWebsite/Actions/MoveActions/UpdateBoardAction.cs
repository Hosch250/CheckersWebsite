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

        public Task Handle(OnMoveNotification request, CancellationToken cancellationToken)
        {
            var lastMoveDate = _mediator.Send(new GetLastMoveDateMessage(request.ViewModel)).Result;

            var blackConnection = GetClientConnection(request.ViewModel.BlackPlayerID);
            var whiteConnection = GetClientConnection(request.ViewModel.WhitePlayerID);

            if (request.ViewModel.BlackPlayerID != ComputerPlayer.ComputerPlayerID && blackConnection != null)
            {
                _signalRHub.Clients.Client(blackConnection).InvokeAsync("UpdateBoard", request.ViewModel.ID, lastMoveDate,
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(request.ViewModel.BlackPlayerID, Player.Black)),
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(request.ViewModel.BlackPlayerID, Player.White)));
                
                _signalRHub.Groups.RemoveAsync(blackConnection, request.ViewModel.ID.ToString()).Wait();
            }

            if (request.ViewModel.WhitePlayerID != ComputerPlayer.ComputerPlayerID && whiteConnection != null)
            {
                _signalRHub.Clients.Client(whiteConnection).InvokeAsync("UpdateBoard", request.ViewModel.ID, lastMoveDate,
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(request.ViewModel.WhitePlayerID, Player.Black)),
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(request.ViewModel.WhitePlayerID, Player.White)));

                _signalRHub.Groups.RemoveAsync(whiteConnection, request.ViewModel.ID.ToString()).Wait();
            }

            _signalRHub.Clients
                .Group(request.ViewModel.ID.ToString())
                .InvokeAsync("UpdateBoard", request.ViewModel.ID, lastMoveDate,
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(Guid.Empty, Player.Black)),
                    ComponentGenerator.GetBoard(request.ViewModel, GetViewData(Guid.Empty, Player.White)));

            if (request.ViewModel.BlackPlayerID != ComputerPlayer.ComputerPlayerID && blackConnection != null)
            {
                _signalRHub.Groups.AddAsync(blackConnection, request.ViewModel.ID.ToString()).Wait();
            }

            if (request.ViewModel.WhitePlayerID != ComputerPlayer.ComputerPlayerID && whiteConnection != null)
            {
                _signalRHub.Groups.AddAsync(whiteConnection, request.ViewModel.ID.ToString()).Wait();
            }

            return Task.CompletedTask;
        }
    }
}