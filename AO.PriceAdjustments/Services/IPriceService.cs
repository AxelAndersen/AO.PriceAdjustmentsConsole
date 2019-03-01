﻿namespace AO.PriceAdjustments.Services
{
    public interface IPriceService
    {
        /// <summary>
        /// Here we create Competitor and CompetitorPriceAdjustments if they dont exist already.
        /// <para>CompetitorPriceAdjustments are used for configuring whether a product can be automatically price adjusted</para>
        /// </summary>
        void EnsureAllEntitiesExist();

        /// <summary>
        /// Here we fetch the data from the PriceShape json file "https://app.priceshape.dk/api/json/products?auth-token=8..."
        /// <para>The path to the json file is configured in appsettings.json</para>
        /// <para>We end up deserializing the json to RootObject containing all products and prices</para>
        /// </summary>
        void GetData();

        /// <summary>
        /// Here we take all CompetitorPrices with new price since last time and add to _newPricedItems
        /// </summary>
        void GetNewPricedItems();

        /// <summary>
        /// Saving prices to [AO.MasterDatabase].[dbo].[CompetitorPrices]
        /// <para>Here we have both last price and the new price.</para>
        /// <para>We also move the former new price to last price column.</para>
        /// </summary>
        void SaveCompetitorPrices();

        /// <summary>
        /// Getting products from our own database for those in question
        /// </summary>
        void GetOwnItems();
    }
}