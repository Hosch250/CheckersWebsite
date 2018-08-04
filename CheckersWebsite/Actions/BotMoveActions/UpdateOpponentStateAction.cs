using CheckersWebsite.SignalR;
using Microsoft.AspNetCore.SignalR;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using CheckersWebsite.Controllers;
using System.Linq;
using System;
using CheckersWebsite.Enums;

namespace CheckersWebsite.Actions.MoveActions
{
    public class SetGameStatusAction : INotificationHandler<OnBotMoveNotification>
    {
        private readonly IHubContext<GameHub> _signalRHub;
        private readonly IMediator _mediator;
        private readonly Database.Context _context;

        public SetGameStatusAction(IHubContext<GameHub> signalRHub, IMediator mediator)
        {
            _signalRHub = signalRHub;
            _mediator = mediator;
        }

        public Task Handle(OnBotMoveNotification request, CancellationToken cancellationToken)
        {
            if (!request.ViewModel.IsBotGame)
            {
                return Task.CompletedTask;
            }

            var moves = request.ViewModel.Turns.OrderByDescending(o => o.BlackMove?.CreatedOn ?? DateTime.MaxValue).ToList();

            var blackMove = moves.FirstOrDefault(w => w.BlackMove != null).BlackMove;
            var whiteMove = moves.FirstOrDefault(w => w.BlackMove != null).WhiteMove;

            if (blackMove != null && whiteMove != null)
            {
                var diff = blackMove.CreatedOn - whiteMove.CreatedOn;

                if (Math.Abs(diff.TotalSeconds) > 5)
                {
                    var lastMover = blackMove.CreatedOn > whiteMove.CreatedOn ? Player.Black : Player.White;

                    _context.Games.Find(request.ViewModel.ID).GameStatus = (lastMover == Player.Black ? (int)Status.WhiteWin : (int)Status.BlackWin);
                    _context.SaveChanges();
                }
            }
            else if (blackMove == null && (whiteMove.CreatedOn - request.ViewModel.CreatedOn).TotalSeconds > 5)
            {
                _context.Games.Find(request.ViewModel.ID).GameStatus = (int)Status.BlackWin;
                _context.SaveChanges();
            }
            else if (whiteMove == null && (blackMove.CreatedOn - request.ViewModel.CreatedOn).TotalSeconds > 5)
            {
                _context.Games.Find(request.ViewModel.ID).GameStatus = (int)Status.WhiteWin;
                _context.SaveChanges();
            }

            return Task.CompletedTask;
        }
    }
}