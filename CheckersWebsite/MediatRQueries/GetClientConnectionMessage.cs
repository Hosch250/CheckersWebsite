using System;
using MediatR;

namespace CheckersWebsite.MediatR
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