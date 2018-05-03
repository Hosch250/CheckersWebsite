using CheckersWebsite.Enums;
using System;

namespace CheckersWebsite.ViewModels
{
    public class GameDisplayViewModel
    {
        public Guid ID { get; set; }
        public Status GameStatus { get; set; }
        public Variant Variant { get; set; }

        public Guid BlackPlayerID { get; set; }
        public Guid WhitePlayerID { get; set; }
    }
}
