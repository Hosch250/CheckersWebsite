using CheckersWebsite.MediatR;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CheckersWebsite.Controllers
{
    public class GetClientConnectionHandler : IRequestHandler<GetClientConnectionMessage, string>
    {
        private readonly Database.Context _context;

        public GetClientConnectionHandler(Database.Context context)
        {
            _context = context;
        }

        public Task<string> Handle(GetClientConnectionMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_context.Players.Find(request.PlayerID).ConnectionID);
        }
    }
}