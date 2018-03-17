using Microsoft.EntityFrameworkCore;

namespace Database
{
    public class Context : DbContext
    {
        public DbSet<Game> Games { get; set; }
        public DbSet<PdnTurn> Turns { get; set; }
        public DbSet<PdnMove> Moves { get; set; }
    }
}
