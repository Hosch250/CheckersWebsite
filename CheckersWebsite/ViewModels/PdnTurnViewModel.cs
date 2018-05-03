using System;
using System.Collections.Generic;

namespace CheckersWebsite.ViewModels
{
    public class PdnTurnViewModel
    {
        public Guid ID { get; set; }

        public int MoveNumber { get; set; }

        public PdnMoveViewModel BlackMove { get; set; }

        public PdnMoveViewModel WhiteMove { get; set; }
    }
}
