using Microsoft.EntityFrameworkCore;

namespace AO.PriceAdjustments.Data
{
    public class MasterContext : DbContext
    {
        public MasterContext(DbContextOptions<MasterContext> options) : base(options)
        { }

        public DbSet<Competitor> Competitor { get; set; }
        public DbSet<CompetitorPriceAdjustments> CompetitorPriceAdjustments { get; set; }
        public DbSet<CompetitorPrices> CompetitorPrices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CompetitorPrices>().HasKey(c => new { c.EAN, c.CompetitorId });
        }
    }
}
