using AO.PriceAdjustments.Data;
using AO.PriceAdjustments.Models;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace AO.PriceAdjustments.Services
{
    public class PriceService
    {
        IConfiguration _config;
        RootObject _root;

        public PriceService()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
            _config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true, true).Build();
        }

        public void GetData()
        {
            string path = _config["PriceShape.JsonPath"];
            string json = string.Empty;

            using (WebClient wc = new WebClient())
            {
                json = wc.DownloadString(path);
            }

            _root = JsonConvert.DeserializeObject<RootObject>(json);
        }

        public void SaveCompetitorPrices()
        {
            using (var context = new MasterContext())
            {
                foreach (Item item in _root.items)
                {
                    foreach (PriceshapeScraper priceshapeScraper in item.priceshape_scraper)
                    {
                        var competitor = context.Competitor.Where(c => c.CompetitorName == priceshapeScraper.name).FirstOrDefault();
                        if (competitor != null)
                        {
                            var competorPrice = context.CompetitorPrices.Where(c => c.EAN == item.gtin && c.CompetitorId == competitor.Id).FirstOrDefault();
                            if (competorPrice == null)
                            {
                                var competitorPrices = new CompetitorPrices
                                {
                                    CompetitorId = competitor.Id,
                                    EAN = item.gtin,
                                    NewPrice = Convert.ToDecimal(priceshapeScraper.price),
                                    NewPriceTime = DateTime.Now,
                                    LastPriceTime = Convert.ToDateTime("01-01-1970")
                                };
                                context.Add(competitorPrices);
                            }
                            else
                            {
                                competorPrice.LastPrice = competorPrice.NewPrice;
                                competorPrice.LastPriceTime = competorPrice.NewPriceTime;
                                competorPrice.NewPrice = Convert.ToDecimal(priceshapeScraper.price);
                                competorPrice.NewPriceTime = DateTime.Now;
                                context.Update(competorPrice);
                            }
                        }
                    }
                }
                context.SaveChanges();
            }
        }

        public void EnsureAllCompetitorsExist()
        {
            foreach (Item item in _root.items)
            {
                foreach (PriceshapeScraper priceshapeScraper in item.priceshape_scraper)
                {
                    using (var context = new MasterContext())
                    {
                        var competitor = context.Competitor.Where(c => c.CompetitorName == priceshapeScraper.name).FirstOrDefault();
                        if (competitor == null)
                        {
                            Competitor newCompetitor = new Competitor()
                            {                                
                                CompetitorName = priceshapeScraper.name
                            };

                            context.Add(newCompetitor);
                            context.SaveChanges();
                        }
                    }
                }
            }
        }
    }
}