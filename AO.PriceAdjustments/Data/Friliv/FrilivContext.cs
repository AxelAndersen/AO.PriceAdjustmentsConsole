using AO.PriceAdjustments.Data.Friliv;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace AO.PriceAdjustments.Data.Friliv
{
    public class FrilivContext : DbContext
    {
        public FrilivContext(DbContextOptions<FrilivContext> options) : base(options)
        { }

        public DbSet<Products> Products { get; set; }

        public DbQuery<ProductIdWithEAN> ProductIdWithEANs { get; set; }
    }
}
