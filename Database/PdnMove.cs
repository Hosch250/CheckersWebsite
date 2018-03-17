using System;
using System.ComponentModel.DataAnnotations;

namespace Database
{
    public class PdnMove
    {
        [Key]
        public Guid ID { get; set; }

        public string Move { get; }
        public string ResultingFen { get; }
        public string DisplayString { get; }
        public int PieceTypeMoved { get; }
        public bool? IsJump { get; }
        
        public virtual PdnTurn Turn { get; set; }
    }
}
