using System;
using System.Collections.Generic;
using System.Text;

namespace AO.PriceAdjustments.Models
{
    public class Item
    {
        public string title { get; set; }
        public string description { get; set; }
        public string brand { get; set; }
        public string price { get; set; }
        public string clear_price { get; set; }
        public string mpn { get; set; }
        public string gtin { get; set; }
        public string gid { get; set; }
        public string link { get; set; }
        public string image_link { get; set; }
        public string availability { get; set; }
        public List<PriceshapeScraper> priceshape_scraper { get; set; }
    }
}
