using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AO.PriceAdjustments.Data
{
    public class CompetitorPrices
    {        
        public string EAN { get; set; }
        
        public int CompetitorId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LastPrice { get; set; }

        public DateTime LastPriceTime { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NewPrice { get; set; }

        public DateTime NewPriceTime { get; set; }
    }
}