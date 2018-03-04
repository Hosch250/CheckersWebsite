using Microsoft.FSharp.Core;

namespace CheckersWebsite.Facade
{
    public class Board
    {
        public Piece[,] GameBoard { get; set; }

        public Board(FSharpOption<Checkers.Piece.Piece>[,] board)
        {
            var value = new Piece[8, 8];
            for (var row = 0; row < 8; row++)
            {
                for (var col = 0; col < 8; col++)
                {
                    value[row, col] = board[row, col].Convert();
                }
            }

            GameBoard = value;
        }

        public Board(Piece[,] board)
        {
            GameBoard = board;
        }

        public Board() { }

        public static Board EmptyBoard() =>
            new Board(Checkers.Board.emptyBoardList());

        public static Board DefaultBoard(Variant variant) =>
            new Board(Checkers.Board.defaultBoard);

        public Board Copy()
        {
            var board = new Piece[8, 8];
            for (var i = 0; i < 8; i++)
            {
                for (var j = 0; j < 8; j++)
                {
                    board[i, j] = GameBoard[i, j];
                }
            }

            return new Board(board);
        }

        public static bool IsValidSquare(Variant variant, int row, int column)
        {
            return row >= 0 && row < 8 &&
                   column >= 0 && column < 8 &&
                   !Equals(Checkers.PublicAPI.pdnBoard(variant.ToGameVariant().pdnMembers)[row, column], FSharpOption<int>.None);
        }

        public Piece this[Coord coord] => GameBoard[coord.Row, coord.Column];

        public Piece this[int row, int column] => GameBoard[row, column];

        public static implicit operator Board(FSharpOption<Checkers.Piece.Piece>[,] value)
        {
            return new Board(value);
        }

        public static implicit operator FSharpOption<Checkers.Piece.Piece>[,](Board value)
        {
            var board = new FSharpOption<Checkers.Piece.Piece>[8, 8];
            for (var row = 0; row < 8; row++)
            {
                for (var col = 0; col < 8; col++)
                {
                    board[row, col] = value.GameBoard[row, col].ConvertBack();
                }
            }

            return board;
        }
    }
}
