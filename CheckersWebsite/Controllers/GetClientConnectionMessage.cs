using System;
using MediatR;

namespace CheckersWebsite.Controllers
{
    public class GetClientConnectionMessage : IRequest<string>
    {
        public GetClientConnectionMessage(Guid playerID)
        {
            PlayerID = playerID;
        }

        public Guid PlayerID { get; }
    }
}