using System;
using System.Collections.Generic;
using System.Linq;
using Checkers;
using CheckersWebsite.Enums;
using Microsoft.FSharp.Core;

namespace CheckersWebsite.Facade
{
    public class PdnMove
    {
        public PdnMove(List<int> move, string resultingFen, string displayString, PieceType? pieceTypeMoved, Player? player, bool? isJump, Guid? id = null)
        {
            Move = move;
            ResultingFen = resultingFen;
            DisplayString = displayString;
            PieceTypeMoved = pieceTypeMoved;
            Player = player;
            IsJump = isJump;
            ID = id;
        }

        public List<int> Move { get; }
        public string ResultingFen { get; }
        public string DisplayString { get; }
        public PieceType? PieceTypeMoved { get; }
        public Player? Player { get; }
        public bool? IsJump { get; }
        public Guid? ID { get; }

        public static implicit operator PdnMove(Generic.PdnMove value)
        {
            return new PdnMove(value.Move.ToList(), value.ResultingFen, value.DisplayString, value.PieceTypeMoved.Convert(), value.Player.Convert(), value.IsJump.Convert());
        }

        public static implicit operator Generic.PdnMove(PdnMove value)
        {
            return new Generic.PdnMove(Generic.listFromSeq(value.Move).Value, value.ResultingFen, value.DisplayString, value.PieceTypeMoved.ConvertBack(), value.Player.ConvertBack(), value.IsJump.ConvertBack());
        }

        public static implicit operator PdnMove(FSharpOption<Generic.PdnMove> value)
        {
            return Equals(value, FSharpOption<Generic.PdnMove>.None)
                ? null
                : new PdnMove(value.Value.Move.ToList(), value.Value.ResultingFen, value.Value.DisplayString, value.Value.PieceTypeMoved.Convert(), value.Value.Player.Convert(), value.Value.IsJump.Convert());
        }

        public static implicit operator FSharpOption<Generic.PdnMove>(PdnMove value)
        {
            return value == null
                ? FSharpOption<Generic.PdnMove>.None
                : new FSharpOption<Generic.PdnMove>(new Generic.PdnMove(Generic.listFromSeq(value.Move).Value, value.ResultingFen, value.DisplayString, value.PieceTypeMoved.ConvertBack(), value.Player.ConvertBack(), value.IsJump.ConvertBack()));
        }
    }

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
