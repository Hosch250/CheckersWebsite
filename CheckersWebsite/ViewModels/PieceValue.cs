﻿using CheckersWebsite.Enums;
using CheckersWebsite.Facade;

namespace CheckersWebsite.ViewModels
{
    public class PieceValue
    {
        public string ID { get; set; }
        public Player Player { get; set; }
        public PieceType Piece { get; set; }
        public Coord Coord { get; set; }
    }
}
