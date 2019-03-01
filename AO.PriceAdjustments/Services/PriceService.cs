using AO.PriceAdjustments.Common;
using AO.PriceAdjustments.Data;
using AO.PriceAdjustments.Data.Friliv;
using AO.PriceAdjustments.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace AO.PriceAdjustments.Services
{
    public class PriceService : IPriceService
    {
        #region Private variables
        private IConfiguration _config;
        private RootObject _root;
        private List<CompetitorPrices> _newPricedItems = null;
        private List<Products> _allFrilivProducts = null;
        private List<Products> _frilivProductsWithNewPrices = null;
        private List<Products> _frilivProductsWithCampaignPrice = null;
        private List<Products> _frilivProductsWithAdjustmentNotAllowed = null;
        private List<Products> _frilivProductsWithStoppedByBarrier = null;
        private List<ProductIdWithEAN> _frilivProductIdWithEANs = null;
        private ILogger<PriceService> _logger;
        private MasterContext _masterContext;
        private FrilivContext _frilivContext;
        private List<CompetitorPrices> _adjustedPrices = new List<CompetitorPrices>();
        private IMailService _mailService;
        #endregion

        private int DaysToSetNewOffer
        {
            get
            {
                return Convert.ToInt32(_config["General:DaysToSetNewOffer"]);
            }
        }

        public PriceService(ILogger<PriceService> logger, SmtpClient smtpClient, IConfiguration config, MasterContext masterContext, FrilivContext frilivContext, IMailService mailService)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            _config = config;
            _logger = logger;
            _masterContext = masterContext;
            _frilivContext = frilivContext;
            _mailService = mailService;
        }

        /// <summary>
        /// Here we fetch the data from the PriceShape json file "https://app.priceshape.dk/api/json/products?auth-token=8..."
        /// <para>The path to the json file is configured in appsettings.json</para>
        /// <para>We end up deserializing the json to RootObject containing all products and prices</para>
        /// </summary>
        public void GetData()
        {
            string path = _config["General:PriceShape.JsonPath"];
            string json = string.Empty;

            using (WebClient wc = new WebClient())
            {
                json = wc.DownloadString(path);
            }

            _root = JsonConvert.DeserializeObject<RootObject>(json);
        }

        /// <summary>
        /// Here we create Competitor and CompetitorPriceAdjustments if they dont exist already.
        /// <para>CompetitorPriceAdjustments are used for configuring whether a product can be automatically price adjusted</para>
        /// </summary>
        public void EnsureAllEntitiesExist()
        {
            foreach (Item item in _root.items)
            {
                foreach (PriceshapeScraper priceshapeScraper in item.priceshape_scraper)
                {
                    var competitor = _masterContext.Competitor.Where(c => c.CompetitorName == priceshapeScraper.name).FirstOrDefault();
                    if (competitor == null)
                    {
                        Competitor newCompetitor = new Competitor()
                        {
                            CompetitorName = priceshapeScraper.name
                        };

                        _masterContext.Add(newCompetitor);
                        _masterContext.SaveChanges();
                    }
                }

                var priceAdjustment = _masterContext.CompetitorPriceAdjustments.Where(c => c.EAN == item.gtin).FirstOrDefault();
                if (priceAdjustment == null)
                {
                    CompetitorPriceAdjustments newPriceAdjustment = new CompetitorPriceAdjustments()
                    {
                        EAN = item.gtin,
                        ProductName = item.brand + " " + item.title,
                        CurrentPrice = Convert.ToDecimal(item.clear_price)
                    };

                    _masterContext.Add(newPriceAdjustment);
                }
                else
                {
                    priceAdjustment.CurrentPrice = Convert.ToDecimal(item.clear_price);
                    priceAdjustment.ProductName = item.brand + " " + item.title;
                    _masterContext.Update(priceAdjustment);
                }
                _masterContext.SaveChanges();
            }
        }

        /// <summary>
        /// Used prepare CompetitorPrices table. 
        /// <para>Here we set the LastPrcie to NewPrice to be ready for this run</para>
        /// </summary>
        public void PreparePrices()
        {
            var allCompetitorPrices = _masterContext.CompetitorPrices.ToList();
            foreach (CompetitorPrices competitorPrice in allCompetitorPrices)
            {
                competitorPrice.LastPrice = competitorPrice.NewPrice;
                competitorPrice.LastPriceTime = competitorPrice.NewPriceTime;
                _masterContext.Update(competitorPrice);
            }
            _masterContext.SaveChanges();
        }

        /// <summary>
        /// Saving prices to [AO.MasterDatabase].[dbo].[CompetitorPrices]
        /// <para>Here we have both last price and the new price.</para>
        /// <para>We also move the former new price to last price column.</para>
        /// </summary>
        public void SaveCompetitorPrices()
        {
            foreach (Item item in _root.items)
            {
                if (item.priceshape_scraper != null && item.priceshape_scraper.Count > 0)
                {
                    PriceshapeScraper priceshapeScraper = GetLowest(item.priceshape_scraper);

                    var competitor = _masterContext.Competitor.Where(c => c.CompetitorName == priceshapeScraper.name).FirstOrDefault();
                    if (competitor != null)
                    {
                        var competorPrice = _masterContext.CompetitorPrices.Where(c => c.EAN == item.gtin && c.CompetitorId == competitor.Id).FirstOrDefault();
                        if (competorPrice == null)
                        {
                            var competitorPrices = new CompetitorPrices
                            {
                                CompetitorId = competitor.Id,
                                EAN = item.gtin,
                                NewPrice = Convert.ToDecimal(priceshapeScraper.clear_price),
                                NewPriceTime = DateTime.Now,
                                LastPriceTime = Convert.ToDateTime("01-01-1970")
                            };
                            _masterContext.Add(competitorPrices);
                        }
                        else
                        {
                            competorPrice.NewPrice = Convert.ToDecimal(priceshapeScraper.clear_price);
                            competorPrice.NewPriceTime = DateTime.Now;
                            _masterContext.CompetitorPrices.Update(competorPrice);
                        }
                    }
                    _masterContext.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Here we take all CompetitorPrices with new price since last time and add to _newPricedItems
        /// </summary>
        public void GetNewPricedItems()
        {
            _newPricedItems = _masterContext.CompetitorPrices.Where(c => c.NewPrice != c.LastPrice).ToList();
        }

        /// <summary>
        /// Getting products from our own database to use for updating prices
        /// </summary>
        public void GetOwnItems()
        {
            _allFrilivProducts = _frilivContext.Products.Where(p => p.ProductStatusId == 1).ToList();
        }

        /// <summary>
        /// Getting a combination of Friliv ProductId and EAN
        /// </summary>
        public void GetFrilivProductIdWithEANs()
        {
            _frilivProductIdWithEANs = _frilivContext.ProductIdWithEANs.ToList();
        }

        /// <summary>
        /// Getting the Friliv products which are in the list of NewPricedItems
        /// <para>Furthermore it will split products up in CampaignPriced and regular prices (Retail or offer)</para>
        /// <para>Lastly it will adjust prices when its allowed</para>
        /// </summary>
        public void AdjustPrices()
        {
            if (_newPricedItems != null && _newPricedItems.Count > 0)
            {
                _frilivProductsWithNewPrices = new List<Products>();

                foreach (CompetitorPrices competitorPrice in _newPricedItems)
                {
                    int productId = _frilivProductIdWithEANs.Where(p => p.EAN == competitorPrice.EAN).Select(p => p.Id).FirstOrDefault();
                    if (productId > 0)
                    {
                        Products product = _allFrilivProducts.Where(p => p.Id == productId).FirstOrDefault();
                        if (product != null)
                        {
                            _frilivProductsWithNewPrices.Add(product);

                            CompetitorPriceAdjustments competitorPriceAdjustment = _masterContext.CompetitorPriceAdjustments.Where(c => c.EAN == competitorPrice.EAN).FirstOrDefault();
                            if (competitorPriceAdjustment != null)
                            {
                                if (competitorPriceAdjustment.CurrentPrice == product.RetailPriceDKK)
                                {
                                    // RetailPrice is our current price
                                    AdjustPrice(competitorPrice, product, competitorPriceAdjustment, product.RetailPriceDKK, true);
                                }
                                else if (competitorPriceAdjustment.CurrentPrice == product.OfferPriceDKK)
                                {
                                    // OfferPrice is our current price
                                    AdjustPrice(competitorPrice, product, competitorPriceAdjustment, product.RetailPriceDKK, false);
                                }
                                else
                                {
                                    // Current price must be some CampaignPrice
                                    if (_frilivProductsWithCampaignPrice == null)
                                    {
                                        _frilivProductsWithCampaignPrice = new List<Products>();
                                    }
                                    _frilivProductsWithCampaignPrice.Add(product);
                                }
                            }
                        }
                    }
                }
            }
        }

        private PriceshapeScraper GetLowest(List<PriceshapeScraper> priceshape_scraper)
        {
            PriceshapeScraper lowestPrice = null;
            foreach (PriceshapeScraper scraper in priceshape_scraper)
            {
                if (lowestPrice == null)
                {
                    lowestPrice = scraper;
                }
                else
                {
                    if (Convert.ToDecimal(lowestPrice.clear_price) > Convert.ToDecimal(scraper.clear_price))
                    {
                        lowestPrice = scraper;
                    }
                }
            }
            return lowestPrice;
        }

        private void AdjustPrice(CompetitorPrices competitorPrice, Products product, CompetitorPriceAdjustments competitorPriceAdjustment, decimal retailPrice, bool newOffer)
        {
            if (competitorPrice.NewPrice > competitorPriceAdjustment.CurrentPrice)
            {
                if (competitorPriceAdjustment.SafetyBarrier > 0)
                {
                    // We don not adjust prices if larger than safety barrier
                    decimal margin = ((competitorPrice.NewPrice - competitorPriceAdjustment.CurrentPrice) / competitorPriceAdjustment.CurrentPrice) * 100;
                    if (margin > competitorPriceAdjustment.SafetyBarrier)
                    {
                        if(_frilivProductsWithStoppedByBarrier == null)
                        {
                            _frilivProductsWithStoppedByBarrier = new List<Products>();
                        }
                        _frilivProductsWithStoppedByBarrier.Add(product);
                        return;
                    }
                }

                if (competitorPriceAdjustment.AllowAutomaticUp == false)
                {
                    if (_frilivProductsWithAdjustmentNotAllowed == null)
                    {
                        _frilivProductsWithAdjustmentNotAllowed = new List<Products>();
                    }
                    _frilivProductsWithAdjustmentNotAllowed.Add(product);
                }
                else
                {
                    if (retailPrice <= competitorPrice.NewPrice)
                    {
                        // If we let the price go up and it will be more than our retail price, just let the retail price take over
                        product.OfferEndDate = DateTime.Now.AddDays(-1);
                    }
                    else
                    {
                        // The price should go up and we are allowed to go up automatically
                        product.OfferPriceDKK = competitorPrice.NewPrice;
                        if (newOffer)
                        {
                            product.OfferEndDate = DateTime.Now.AddDays(DaysToSetNewOffer);
                        }
                    }
                    _frilivContext.Update(product);
                    _frilivContext.SaveChanges();
                    _adjustedPrices.Add(competitorPrice);
                }
            }
            else if (competitorPrice.NewPrice < competitorPriceAdjustment.CurrentPrice)
            {
                if (competitorPriceAdjustment.SafetyBarrier > 0)
                {
                    // We don not adjust prices if larger than safety barrier
                    decimal margin = ((competitorPriceAdjustment.CurrentPrice - competitorPrice.NewPrice) / competitorPriceAdjustment.CurrentPrice) * 100;
                    if (margin > competitorPriceAdjustment.SafetyBarrier)
                    {
                        if (_frilivProductsWithStoppedByBarrier == null)
                        {
                            _frilivProductsWithStoppedByBarrier = new List<Products>();
                        }
                        _frilivProductsWithStoppedByBarrier.Add(product);
                        return;
                    }
                }

                if (competitorPriceAdjustment.AllowAutomaticDown == false)
                {
                    if (_frilivProductsWithAdjustmentNotAllowed == null)
                    {
                        _frilivProductsWithAdjustmentNotAllowed = new List<Products>();
                    }
                    _frilivProductsWithAdjustmentNotAllowed.Add(product);
                }
                else
                {
                    // The price should go up and we are allowed to go up automatically
                    product.OfferPriceDKK = competitorPrice.NewPrice;
                    if (newOffer)
                    {
                        product.OfferEndDate = DateTime.Now.AddDays(DaysToSetNewOffer);
                    }
                    _frilivContext.Update(product);
                    _frilivContext.SaveChanges();
                    _adjustedPrices.Add(competitorPrice);
                }
            }
        }

        public void SendStatusMail()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_config["Status:Header"]);
            sb.Append("<br /><br />");

            if (_adjustedPrices != null && _adjustedPrices.Count > 0)
            {
                sb.Append("Prices automatically adjusted:");
                sb.Append("<br />");

                // Prices adjusted
                foreach (CompetitorPrices competitorPrice in _adjustedPrices)
                {
                    var frilivItem = _frilivProductIdWithEANs.Where(p => p.EAN == competitorPrice.EAN).FirstOrDefault();
                    if (frilivItem != null)
                    {
                        var frilivProduct = _frilivContext.Products.Where(p => p.Id == frilivItem.Id).FirstOrDefault();
                        if (frilivProduct != null)
                        {
                            sb.Append(frilivProduct.Id + " " + frilivProduct.Title);
                            var adjustments = _masterContext.CompetitorPriceAdjustments.Where(c => c.EAN == competitorPrice.EAN).FirstOrDefault();
                            if (adjustments != null)
                            {
                                sb.Append(", Price before: " + adjustments.CurrentPrice.Presentation());
                            }
                            sb.Append(" changed to: " + competitorPrice.NewPrice.Presentation());

                            var competitor = _masterContext.Competitor.Where(c => c.Id == competitorPrice.CompetitorId).FirstOrDefault();
                            if (competitor != null)
                            {
                                sb.Append(" (" + competitor.CompetitorName + ")");
                            }
                        }
                        sb.Append("<br />");
                    }
                }
            }

            if (_frilivProductsWithCampaignPrice != null && _frilivProductsWithCampaignPrice.Count > 0)
            {
                sb.Append("<br /><hr />");
                sb.Append("Prices not touched due to campaign prices:");
                sb.Append("<br />");
                // Campaign prices
                foreach (Products product in _frilivProductsWithCampaignPrice)
                {
                    sb.Append(product.Id + " " + product.Title + "<br />");
                }
            }

            if (_frilivProductsWithAdjustmentNotAllowed != null && _frilivProductsWithAdjustmentNotAllowed.Count > 0)
            {
                sb.Append("<br /><hr />");
                sb.Append("Prices not touched due to automatic adjustment not allowed:");
                sb.Append("<br />");
                // Not allowed prices
                foreach (Products product in _frilivProductsWithAdjustmentNotAllowed)
                {
                    sb.Append(product.Id + " " + product.Title + "<br />");
                }
            }

            if (_frilivProductsWithStoppedByBarrier != null && _frilivProductsWithStoppedByBarrier.Count > 0)
            {
                sb.Append("<br /><hr />");
                sb.Append("Prices not touched due to barrier:");
                sb.Append("<br />");
                // Not allowed prices
                foreach (Products product in _frilivProductsWithStoppedByBarrier)
                {
                    sb.Append(product.Id + " " + product.Title + "<br />");
                }
            }

            _mailService.SendMail("Price adjustment status", sb.ToString(), _config["Status:MailTo"]);
        }
    }
}