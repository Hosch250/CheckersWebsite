namespace CheckersWebsite.Facade
{
    public class Piece
    {
        public Player Player { get; }
        public PieceType PieceType { get; }

        public Piece(Player player, PieceType pieceType)
        {
            Player = player;
            PieceType = pieceType;
        }

        public static Piece WhiteChecker =>
            new Piece(Player.White, PieceType.Checker);

        public static Piece WhiteKing =>
            new Piece(Player.White, PieceType.King);

        public static Piece BlackChecker =>
            new Piece(Player.Black, PieceType.Checker);

        public static Piece BlackKing =>
            new Piece(Player.Black, PieceType.King);

        public override bool Equals(object obj)
        {
            if (obj == null || typeof(Piece) != obj.GetType())
            {
                return false;
            }

            var value = (Piece)obj;
            return Player.Equals(value.Player) &&
                   PieceType.Equals(value.PieceType);
        }

        public override int GetHashCode() => Player.GetHashCode() ^ PieceType.GetHashCode();
    }
}
