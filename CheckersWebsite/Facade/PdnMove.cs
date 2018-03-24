using System.Collections.Generic;
using System.Linq;
using Checkers;
using Microsoft.FSharp.Core;

namespace CheckersWebsite.Facade
{
    public class PdnMove
    {
        public PdnMove(List<int> move, string resultingFen, string displayString, PieceType? pieceTypeMoved, bool? isJump)
        {
            Move = move;
            ResultingFen = resultingFen;
            DisplayString = displayString;
            PieceTypeMoved = pieceTypeMoved;
            Player = string.IsNullOrEmpty(resultingFen) ? null : resultingFen.Split('"')[1].Split(':')[0] == "W" ? (Player?) Facade.Player.Black : (Player?) Facade.Player.White;
            IsJump = isJump;
        }

        public List<int> Move { get; }
        public string ResultingFen { get; }
        public string DisplayString { get; }
        public PieceType? PieceTypeMoved { get; }
        public Player? Player { get; }
        public bool? IsJump { get; }

        public static implicit operator PdnMove(Generic.PdnMove value)
        {
            return new PdnMove(value.Move.ToList(), value.ResultingFen, value.DisplayString, value.PieceTypeMoved.Convert(), value.IsJump.Convert());
        }

        public static implicit operator Generic.PdnMove(PdnMove value)
        {
            return new Generic.PdnMove(Generic.listFromSeq(value.Move).Value, value.ResultingFen, value.DisplayString, value.PieceTypeMoved.ConvertBack(), value.IsJump.ConvertBack());
        }

        public static implicit operator PdnMove(FSharpOption<Generic.PdnMove> value)
        {
            return Equals(value, FSharpOption<Generic.PdnMove>.None)
                ? null
                : new PdnMove(value.Value.Move.ToList(), value.Value.ResultingFen, value.Value.DisplayString, value.Value.PieceTypeMoved.Convert(), value.Value.IsJump.Convert());
        }

        public static implicit operator FSharpOption<Generic.PdnMove>(PdnMove value)
        {
            return value == null
                ? FSharpOption<Generic.PdnMove>.None
                : new FSharpOption<Generic.PdnMove>(new Generic.PdnMove(Generic.listFromSeq(value.Move).Value, value.ResultingFen, value.DisplayString, value.PieceTypeMoved.ConvertBack(), value.IsJump.ConvertBack()));
        }
    }

    public static class PdnMoveExtensions
    {
        public static PdnMove ToPdnMove(this Database.PdnMove move)
        {
            var pdnMove = new PdnMove(move.Move.Split(',').Select(int.Parse).ToList(), move.ResultingFen, move.DisplayString, (PieceType) move.PieceTypeMoved, move.IsJump);

            return pdnMove;
        }

        public static Database.PdnMove ToPdnMove(this PdnMove move)
        {
            var pdnMove = new Database.PdnMove
            {
                DisplayString = move.DisplayString,
                IsJump = move.IsJump,
                Move = string.Join(',', move.Move),
                PieceTypeMoved = (int) move.PieceTypeMoved,
                ResultingFen = move.ResultingFen,
                Player = (int) move.Player
            };

            return pdnMove;
        }
    }
}
