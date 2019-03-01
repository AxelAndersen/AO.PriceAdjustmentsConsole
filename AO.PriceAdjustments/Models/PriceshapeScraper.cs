using System;
using System.Collections.Generic;
using System.Text;

namespace AO.PriceAdjustments.Models
{
    public class PriceshapeScraper
    {
        public int id { get; set; }
        public string clear_price { get; set; }
        public string price { get; set; }
        public string name { get; set; }
    }

}
