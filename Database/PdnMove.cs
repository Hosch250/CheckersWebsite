using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database
{
    public class PdnMove
    {
        [Key]
        public Guid ID { get; set; }

        public string Move { get; set; }
        public string ResultingFen { get; set; }
        public string DisplayString { get; set; }
        public int? PieceTypeMoved { get; set; }
        public int? Player { get; set; }
        public bool? IsJump { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedOn { get; set; }

        public Guid TurnID { get; set; }

        [ForeignKey(nameof(TurnID))]
        public PdnTurn Turn { get; set; }
    }
}
