using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Database
{
    public class Context : DbContext
    {
        public DbSet<Game> Games { get; set; }
        public DbSet<PdnTurn> Turns { get; set; }
        public DbSet<PdnMove> Moves { get; set; }

        public Context() : base()
        {
        }

        public Context(DbContextOptions options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
        }
    }
}
