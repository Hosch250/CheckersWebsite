﻿using CheckersWebsite.Enums;
using System;
using System.Collections.Generic;

namespace CheckersWebsite.ViewModels
{
    public class GameViewModel
    {
        public Guid ID { get; set; }

        public Variant Variant { get; set; }
        public Player CurrentPlayer { get; set; }
        public string InitialPosition { get; set; }
        public int CurrentPosition { get; set; }
        public string Fen { get; set; }
        public Status GameStatus { get; set; }
        public bool DisplayingLastMove { get; set; }

        public Guid BlackPlayerID { get; set; }
        public Guid WhitePlayerID { get; set; }

        public int BlackPlayerStrength { get; set; }
        public int WhitePlayerStrength { get; set; }

        public List<PdnTurnViewModel> Turns { get; set; }
        public BoardViewModel Board { get; set; }
    }
}
