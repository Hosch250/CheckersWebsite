using Checkers;
using Microsoft.FSharp.Core;

namespace CheckersWebsite.Facade
{
    public class Coord
    {
        public Coord() { }

        public int Row { get; set; }
        public int Column { get; set; }

        public Coord(int row, int column)
        {
            Row = row;
            Column = column;
        }

        public Coord Offset(Coord coord) =>
            new Coord(Row + coord.Row, Column + coord.Column);

        public static implicit operator Coord(Generic.Coord coord)
        {
            return new Coord(coord.Row, coord.Column);
        }

        public static implicit operator Generic.Coord(Coord coord)
        {
            return new Generic.Coord(coord.Row, coord.Column);
        }

        public static implicit operator Coord(FSharpOption<Generic.Coord> coord)
        {
            return Equals(coord, FSharpOption<Generic.Coord>.None)
                ? null
                : new Coord(coord.Value.Row, coord.Value.Column);
        }

        public static implicit operator FSharpOption<Generic.Coord>(Coord coord)
        {
            return coord == null
                ? FSharpOption<Generic.Coord>.None
                : FSharpOption<Generic.Coord>.Some(new Generic.Coord(coord.Row, coord.Column));
        }

        public override bool Equals(object obj)
        {
            if (obj == null || typeof(Coord) != obj.GetType())
            {
                return false;
            }

            var value = (Coord)obj;
            return Row == value.Row && Column == value.Column;
        }

        public override int GetHashCode() => Row.GetHashCode() ^ Column.GetHashCode();
    }
}
