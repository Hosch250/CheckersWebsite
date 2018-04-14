using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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

        public virtual ICollection<PdnTurn> Turns { get; set; }
    }
}
