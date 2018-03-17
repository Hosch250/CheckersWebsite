using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database
{
    public class PdnTurn
    {
        [Key]
        public Guid ID { get; set; }

        public int MoveNumber { get; }
        
        public Guid GameID { get; set; }
        public Guid BlackMoveID { get; set; }
        public Guid WhiteMoveID { get; set; }

        [ForeignKey(nameof(GameID))]
        public virtual Game Game { get; }

        [ForeignKey(nameof(BlackMoveID))]
        public virtual PdnMove BlackMove { get; }

        [ForeignKey(nameof(WhiteMoveID))]
        public virtual PdnMove WhiteMove { get; }
    }
}
