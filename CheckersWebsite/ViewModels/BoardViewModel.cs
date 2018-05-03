using CheckersWebsite.Enums;
using CheckersWebsite.Extensions;
using CheckersWebsite.Facade;
using System.Linq;

namespace CheckersWebsite.ViewModels
{
    public class BoardViewModel
    {
        public Piece[,] GameBoard { get; set; }

        public static BoardViewModel EmptyBoard() =>
            new BoardViewModel { GameBoard = Checkers.Board.emptyBoardList().ToArray() };

        public static BoardViewModel DefaultBoard(Variant variant) =>
            new BoardViewModel { GameBoard = Checkers.Board.defaultBoard.ToArray() };

        public Piece this[int row, int column] => GameBoard[row, column];
    }
}
