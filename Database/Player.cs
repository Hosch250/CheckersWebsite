using System;
using System.ComponentModel.DataAnnotations;

namespace Database
{
    public class Player
    {
        [Key]
        public Guid ID { get; set; }

        public string ConnectionID { get; set; }
    }
}
