using System.Linq;
using CheckersWebsite.Enums;
using CheckersWebsite.Facade;

namespace CheckersWebsite.Extensions
{
    public static class PdnMoveExtensions
    {
        public static PdnMove ToPdnMove(this Database.PdnMove move)
        {
            var pdnMove = new PdnMove(
                move.Move.Split(',').Select(int.Parse).ToList(),
                move.ResultingFen,
                move.DisplayString,
                (PieceType?)move.PieceTypeMoved,
                (Player?)move.Player,
                move.IsJump,
                move.ID);

            return pdnMove;
        }

        public static Database.PdnMove ToPdnMove(this PdnMove move)
        {
            var pdnMove = new Database.PdnMove
            {
                DisplayString = move.DisplayString,
                IsJump = move.IsJump,
                Move = string.Join(',', move.Move),
                PieceTypeMoved = (int?)move.PieceTypeMoved.Value,
                ResultingFen = move.ResultingFen,
                Player = (int?)move.Player.Value
            };

            return pdnMove;
        }
    }
}
