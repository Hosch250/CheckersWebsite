using Newtonsoft.Json;
using System;

namespace CheckersWebsite.ViewModels
{
    public class PdnMoveViewModel
    {
        public Guid ID { get; set; }

        public string Move { get; set; }
        public string ResultingFen { get; set; }
        public string DisplayString { get; set; }
        public int? PieceTypeMoved { get; set; }
        public int? Player { get; set; }
        public bool? IsJump { get; set; }
        
        public DateTime CreatedOn { get; set; }
    }
}
