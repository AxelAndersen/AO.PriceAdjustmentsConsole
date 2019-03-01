using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AO.PriceAdjustments.Data
{
    public class CompetitorPriceAdjustments
    {
        [Key]
        public string EAN { get; set; }

        public string ProductName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentPrice { get; set; }

        public bool AllowAutomaticUp { get; set; }

        public bool AllowAutomaticDown { get; set; }

        public int SafetyBarrier { get; set; }
    }
}