﻿using AO.PriceAdjustments.Data;
using AO.PriceAdjustments.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace AO.PriceAdjustments.Services
{
    public class PriceService : IPriceService
    {
        private IConfiguration _config;
        private RootObject _root;
        private List<CompetitorPrices> _newPricedItems = null;
        private ILogger<PriceService> _logger;
        private MasterContext _masterContext;

        public PriceService(ILogger<PriceService> logger, SmtpClient smtpClient, IConfiguration config, MasterContext masterContext)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            _config = config; // new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
            _logger = logger;
            _masterContext = masterContext;
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
        /// Saving prices to [AO.MasterDatabase].[dbo].[CompetitorPrices]
        /// <para>Here we have both last price and the new price.</para>
        /// <para>We also move the former new price to last price column.</para>
        /// </summary>
        public void SaveCompetitorPrices()
        {
            foreach (Item item in _root.items)
            {
                foreach (PriceshapeScraper priceshapeScraper in item.priceshape_scraper)
                {
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
                            competorPrice.LastPrice = competorPrice.NewPrice;
                            competorPrice.LastPriceTime = competorPrice.NewPriceTime;
                            competorPrice.NewPrice = Convert.ToDecimal(priceshapeScraper.clear_price);
                            competorPrice.NewPriceTime = DateTime.Now;
                            _masterContext.Update(competorPrice);
                        }
                    }
                }
                _masterContext.SaveChanges();
            }
        }

        /// <summary>
        /// Here we take all CompetitorPrices with new price since last time and add to _newPricedItems
        /// </summary>
        public void GetNewPricedItems()
        {
            _newPricedItems = _masterContext.CompetitorPrices.Where(c => c.NewPrice != c.LastPrice).ToList();
        }

        public void GetOwnItems()
        {
            
        }
    }
}