using CheckersWebsite.Enums;
using System.Collections.Generic;
using System.Linq;
using CheckersWebsite.Facade;
using CheckersWebsite.ViewModels;

namespace CheckersWebsite.Extensions
{
    public static class PdnTurnExtensions
    {
        public static PdnTurn ToPdnTurn(this Database.PdnTurn turn)
        {
            var pdnTurn = new PdnTurn(turn.MoveNumber, turn.Moves.FirstOrDefault(f => f.Player == (int) Player.Black)?.ToPdnMove(), turn.Moves.FirstOrDefault(f => f.Player == (int)Player.White)?.ToPdnMove());

            return pdnTurn;
        }

        public static Database.PdnTurn ToPdnTurn(this PdnTurn turn)
        {
            var moves = new List<Database.PdnMove>();
            if (turn.BlackMove != null)
            {
                moves.Add(turn.BlackMove.ToPdnMove());
            }
            if (turn.WhiteMove != null)
            {
                moves.Add(turn.WhiteMove.ToPdnMove());
            }

            var pdnTurn = new Database.PdnTurn
            {
                MoveNumber = turn.MoveNumber,
                Moves = moves
            };

            return pdnTurn;
        }

        public static PdnTurnViewModel ToPdnTurnViewModel(this Database.PdnTurn turn)
        {
            return new PdnTurnViewModel
            {
                ID = turn.ID,
                MoveNumber = turn.MoveNumber,
                BlackMove = turn.Moves.SingleOrDefault(s => s.Player == (int)Player.Black)?.ToPdnMoveViewModel(),
                WhiteMove = turn.Moves.SingleOrDefault(s => s.Player == (int)Player.White)?.ToPdnMoveViewModel()
            };
        }
    }
}
