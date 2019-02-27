using System.ComponentModel.DataAnnotations;

namespace AO.PriceAdjustments.Data
{
    public class CompetitorPriceAdjustments
    {
        [Key]
        public string EAN { get; set; }

        public string ProductName { get; set; }

        public bool AllowAutomaticUp { get; set; }

        public bool AllowAutomaticDown { get; set; }

        public int SafetyBarrier { get; set; }
    }
}