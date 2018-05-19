using System;
using Checkers;
using CheckersWebsite.Enums;
using Microsoft.FSharp.Core;
using Player = CheckersWebsite.Enums.Player;
using Piece = CheckersWebsite.Facade.Piece;

namespace CheckersWebsite.Extensions
{
    public static class GeneralExtensions
    {
        public static Player Convert(this Generic.Player value) =>
            value.IsBlack ? Player.Black : Player.White;

        public static Generic.Player ConvertBack(this Player value) =>
            value == Player.Black ? Generic.Player.Black : Generic.Player.White;

        public static Variant Convert(this Generic.Variant value)
        {
            if (value.IsAmericanCheckers)
            {
                return Variant.AmericanCheckers;
            }
            else if (value.IsPoolCheckers)
            {
                return Variant.PoolCheckers;
            }
            else if (value.IsAmericanCheckersOptionalJump)
            {
                return Variant.AmericanCheckersOptionalJump;
            }

            throw new ArgumentException("Unknown variant", nameof(value));
        }

        public static Generic.Variant ConvertBack(this Variant value)
        {
            if (value == Variant.AmericanCheckers)
            {
                return Generic.Variant.AmericanCheckers;
            }
            else if (value == Variant.PoolCheckers)
            {
                return Generic.Variant.PoolCheckers;
            }
            else if (value == Variant.AmericanCheckersOptionalJump)
            {
                return Generic.Variant.AmericanCheckersOptionalJump;
            }

            throw new ArgumentException("Unknown variant", nameof(value));
        }

        public static PieceType Convert(this Generic.PieceType value) =>
            Equals(value, Generic.PieceType.Checker) ? PieceType.Checker : PieceType.King;

        public static Generic.PieceType ConvertBack(this PieceType value) =>
            value == PieceType.Checker ? Generic.PieceType.Checker : Generic.PieceType.King;

        public static Variant ToVariant(this GameVariant.GameVariant gameVariant)
        {
            if (gameVariant.variant.IsAmericanCheckers)
            {
                return Variant.AmericanCheckers;
            }
            else if (gameVariant.variant.IsPoolCheckers)
            {
                return Variant.PoolCheckers;
            }
            else if (gameVariant.variant.IsAmericanCheckersOptionalJump)
            {
                return Variant.AmericanCheckersOptionalJump;
            }

            throw new ArgumentException("Unknown variant", nameof(gameVariant));
        }

        public static GameVariant.GameVariant ToGameVariant(this Variant variant)
        {
            switch (variant)
            {
                case Variant.AmericanCheckers:
                    return GameVariant.GameVariant.AmericanCheckers;
                case Variant.PoolCheckers:
                    return GameVariant.GameVariant.PoolCheckers;
                case Variant.AmericanCheckersOptionalJump:
                    return GameVariant.GameVariant.AmericanCheckersOptionalJump;
                default:
                    throw new System.ArgumentException(nameof(variant));
            }
        }

        public static Piece Convert(this FSharpOption<Checkers.Piece.Piece> piece)
        {
            if (Equals(piece, Checkers.Piece.whiteChecker))
            {
                return Piece.WhiteChecker;
            }
            if (Equals(piece, Checkers.Piece.whiteKing))
            {
                return Piece.WhiteKing;
            }
            if (Equals(piece, Checkers.Piece.blackChecker))
            {
                return Piece.BlackChecker;
            }
            if (Equals(piece, Checkers.Piece.blackKing))
            {
                return Piece.BlackKing;
            }

            return null;
        }

        public static FSharpOption<Checkers.Piece.Piece> ConvertBack(this Piece piece)
        {
            if (Equals(piece, Piece.WhiteChecker))
            {
                return Checkers.Piece.whiteChecker;
            }
            if (Equals(piece, Piece.WhiteKing))
            {
                return Checkers.Piece.whiteKing;
            }
            if (Equals(piece, Piece.BlackChecker))
            {
                return Checkers.Piece.blackChecker;
            }
            if (Equals(piece, Piece.BlackKing))
            {
                return Checkers.Piece.blackKing;
            }

            return FSharpOption<Checkers.Piece.Piece>.None;
        }

        public static PieceType? Convert(this FSharpOption<Generic.PieceType> pieceType)
        {
            if (pieceType == null)
            {
                return null;
            }

            return pieceType.Value.IsChecker ? PieceType.Checker : PieceType.King;
        }

        public static FSharpOption<Generic.PieceType> ConvertBack(this PieceType? pieceType)
        {
            if (pieceType == null)
            {
                return FSharpOption<Generic.PieceType>.None;
            }

            switch (pieceType.Value)
            {
                case PieceType.Checker:
                    return Generic.PieceType.Checker;
                case PieceType.King:
                    return Generic.PieceType.King;
                default:
                    throw new ArgumentException(nameof(pieceType));
            }
        }

        public static bool? Convert(this FSharpOption<bool> value) =>
            Equals(value, FSharpOption<bool>.None)
            ? null
            : new bool?(value.Value);

        public static FSharpOption<bool> ConvertBack(this bool? value) =>
            value == null
            ? FSharpOption<bool>.None
            : new FSharpOption<bool>(value.Value);


        public static Player? Convert(this FSharpOption<Generic.Player> value)
        {
            if (value == null)
            {
                return null;
            }

            return value.Value.IsBlack ? Player.Black : Player.White;
        }

        public static FSharpOption<Generic.Player> ConvertBack(this Player? value)
        {
            if (value == null)
            {
                return FSharpOption<Generic.Player>.None;
            }

            switch (value.Value)
            {
                case Player.White:
                    return Generic.Player.White;
                case Player.Black:
                    return Generic.Player.Black;
                default:
                    throw new ArgumentException(nameof(value));
            }
        }
    }
}
