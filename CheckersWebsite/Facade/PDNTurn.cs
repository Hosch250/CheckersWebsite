using Checkers;

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
}
