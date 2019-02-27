using System.ComponentModel.DataAnnotations;

namespace AO.PriceAdjustments.Data
{
    public class Competitor
    {
        [Key]
        public int Id { get; set; }

        public string CompetitorName { get; set; }
    }
}
