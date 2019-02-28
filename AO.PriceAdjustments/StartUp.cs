using AO.PriceAdjustments.Services;
using Microsoft.Extensions.Logging;
using System;

namespace AO.PriceAdjustments
{
    public class StartUp
    {
        private readonly ILogger<StartUp> _logger;

        public StartUp(ILogger<StartUp> logger)
        {
            _logger = logger;
        }

        public void Run()
        {
            _logger.LogDebug(20, "Running price adjustments");

            string errorMessage = "Error calling PriceService constructor";
            try
            {
                PriceService priceService = new PriceService();

                errorMessage = "Error getting data from PriceShape service";
                priceService.GetData();

                errorMessage = "Error ensuring all data exist in MasterDatabase";
                priceService.EnsureAllEntitiesExist();

                errorMessage = "Error saving prices to CompetitorPrices in MasterDatabase";
                priceService.SaveCompetitorPrices();

                errorMessage = "Error getting new priced items";
                priceService.GetNewPricedItems();


            }
            catch (Exception ex)
            {
                errorMessage += Environment.NewLine + ex.Message;
                errorMessage += Environment.NewLine + ex.ToString();                
                _logger.LogError(errorMessage);
            }            
        }
    }
}
