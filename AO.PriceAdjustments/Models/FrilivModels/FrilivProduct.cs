using System;
using System.Collections.Generic;
using System.Text;

namespace AO.PriceAdjustments.Models.FrilivModels
{
    public class FrilivProduct
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public decimal RetailPriceDKK { get; set; }

        public decimal OfferPriceDKK { get; set; }

        public bool OnSale { get; set; }

        public DateTime OfferEndDate { get; set; }  
    }
}
