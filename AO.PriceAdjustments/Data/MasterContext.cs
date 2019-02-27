using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AO.PriceAdjustments.Data
{
    public class MasterContext : DbContext
    {
        IConfiguration _config;

        public MasterContext()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            _config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
        }

        public DbSet<Competitor> Competitor { get; set; }
        public DbSet<CompetitorPriceAdjustments> CompetitorPriceAdjustments { get; set; }
        public DbSet<CompetitorPrices> CompetitorPrices { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_config["MasterDatabaseConnection"]);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CompetitorPrices>().HasKey(c => new { c.EAN, c.CompetitorId });
        }
    }
}
