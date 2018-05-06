using System;
using System.Linq;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CheckersWebsite.Controllers
{
    public class GetLastMoveDateHandler : IRequestHandler<GetLastMoveDateMessage, DateTime>
    {
        public Task<DateTime> Handle(GetLastMoveDateMessage request, CancellationToken cancellationToken)
        {
            var lastTurn = request.ViewModel.Turns.Last();
            DateTime lastMoveDate;
            if (lastTurn.BlackMove == null)
            {
                lastMoveDate = lastTurn.WhiteMove.CreatedOn;
            }
            else if (lastTurn.WhiteMove == null)
            {
                lastMoveDate = lastTurn.BlackMove.CreatedOn;
            }
            else
            {
                lastMoveDate = lastTurn.BlackMove.CreatedOn > lastTurn.WhiteMove.CreatedOn ? lastTurn.BlackMove.CreatedOn : lastTurn.WhiteMove.CreatedOn;
            }

            return Task.FromResult(lastMoveDate);
        }
    }
}