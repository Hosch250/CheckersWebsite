using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database
{
    public class Game
    {
        [Key]
        public Guid ID { get; set; }

        public int Variant { get; set; }
        public int CurrentPlayer { get; set; }
        public string InitialPosition { get; set; }
        public int CurrentPosition { get; set; }
        public string Fen { get; set; }
        public int GameStatus { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime CreatedOn { get; set; }

        public virtual ICollection<PdnTurn> Turns { get; set; }
    }
}
