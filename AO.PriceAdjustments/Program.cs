using AO.PriceAdjustments.Models;
using AO.PriceAdjustments.Services;
using System;

namespace AO.PriceAdjustments
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                PriceService priceService = new PriceService();
                priceService.GetData();
                priceService.EnsureAllEntitiesExist();
                priceService.SaveCompetitorPrices();
                priceService.GetNewPricedItems();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.Read();
        }
    }
}
