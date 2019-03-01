using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace AO.PriceAdjustments.Data.Friliv
{
    public class Products
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RetailPriceDKK { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal OfferPriceDKK { get; set; }

        public bool OnSale { get; set; }

        public DateTime OfferEndDate { get; set; }

        public int ProductStatusId { get; set; }
    }
}
