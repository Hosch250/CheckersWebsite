using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database
{
    public class PdnTurn
    {
        [Key]
        public Guid ID { get; set; }

        public int MoveNumber { get; set; }
        
        public Guid GameID { get; set; }

        [ForeignKey(nameof(GameID))]
        public virtual Game Game { get; set; }

        public ICollection<PdnMove> Moves { get; set; }
    }
}
