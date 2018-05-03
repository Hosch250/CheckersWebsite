using CheckersWebsite.Facade;
using Microsoft.FSharp.Core;

namespace CheckersWebsite.Extensions
{
    public static class BoardExtensions
    {
        public static Piece[,] ToArray(this FSharpOption<Checkers.Piece.Piece>[,] board)
        {
            var value = new Piece[8, 8];
            for (var row = 0; row < 8; row++)
            {
                for (var col = 0; col < 8; col++)
                {
                    value[row, col] = board[row, col].Convert();
                }
            }

            return value;
        }
    }
}
