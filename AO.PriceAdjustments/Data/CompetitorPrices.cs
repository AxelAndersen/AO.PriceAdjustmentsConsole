using System;
using System.ComponentModel.DataAnnotations;

namespace AO.PriceAdjustments.Data
{
    public class CompetitorPrices
    {        
        public string EAN { get; set; }
        
        public int CompetitorId { get; set; }

        public decimal LastPrice { get; set; }

        public DateTime LastPriceTime { get; set; }

        public decimal NewPrice { get; set; }

        public DateTime NewPriceTime { get; set; }
    }
}