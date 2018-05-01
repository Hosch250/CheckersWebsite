using Checkers;
using CheckersWebsite.Enums;
using System.Collections.Generic;
using System.Linq;

namespace CheckersWebsite.Facade
{
    public class PdnTurn
    {
        public PdnTurn(int moveNumber, PdnMove blackMove, PdnMove whiteMove)
        {
            MoveNumber = moveNumber;
            BlackMove = blackMove;
            WhiteMove = whiteMove;
        }

        public int MoveNumber { get; }
        public PdnMove BlackMove { get; }
        public PdnMove WhiteMove { get; }

        public static implicit operator PdnTurn(Generic.PdnTurn value)
        {
            return new PdnTurn(value.MoveNumber, value.BlackMove, value.WhiteMove);
        }

        public static implicit operator Generic.PdnTurn(PdnTurn value)
        {
            return new Generic.PdnTurn(value.MoveNumber, value.BlackMove, value.WhiteMove);
        }
    }

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
    }
}
