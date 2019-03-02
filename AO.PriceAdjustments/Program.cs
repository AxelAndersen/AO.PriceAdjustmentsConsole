using AO.PriceAdjustments.Data;
using AO.PriceAdjustments.Data.Friliv;
using AO.PriceAdjustments.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace AO.PriceAdjustments
{
    class Program
    {
        private static IServiceProvider _serviceProvider;
        public static IConfigurationRoot Configuration { get; set; }
        public static ILogger<Program> _logger; 

        static void Main(string[] args)
        {
            RegisterServices();

            _logger.LogWarning("AO.PriceAdjustments started");

            string errorMessage = "Error calling PriceService constructor";
            try
            {
                var priceService = _serviceProvider.GetService<IPriceService>();

                errorMessage = "Error getting data from PriceShape service";
                priceService.GetData();

                errorMessage = "Error ensuring all data exist in MasterDatabase";
                priceService.EnsureAllEntitiesExist();

                errorMessage = "Error praparing CompetitorPrices";
                priceService.PreparePrices();

                errorMessage = "Error saving prices to CompetitorPrices in MasterDatabase";
                priceService.SaveCompetitorPrices();

                errorMessage = "Error getting new priced items";
                priceService.GetNewPricedItems();

                errorMessage = "Error getting new own items";
                priceService.GetOwnItems();

                errorMessage = "Error getting ProductIdWithEANs";
                priceService.GetFrilivProductIdWithEANs();

                errorMessage = "Error adjusting prices";
                priceService.AdjustPrices();

                errorMessage = "Error sending status mail";
                priceService.SendStatusMail();
            }
            catch (Exception ex)
            {
                errorMessage += Environment.NewLine + ex.Message;
                errorMessage += Environment.NewLine + ex.ToString();
                
                _logger.LogError(errorMessage);

                var mailService = _serviceProvider.GetService<IMailService>();
                mailService.SendMail("Error: " + ex.Message, errorMessage, "axel@friliv.dk");
            }
            
            DisposeServices();
        }

        private static void RegisterServices()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json");

            Configuration = configBuilder.Build();

            var collection = new ServiceCollection();
            collection
                .AddScoped<IPriceService, PriceService>()
                .AddScoped<IMailService, MailService>()
                .AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Trace);
                        builder.AddNLog(new NLogProviderOptions
                        {
                            CaptureMessageTemplates = true,
                            CaptureMessageProperties = true
                        });
                    })
                .AddSingleton<IConfiguration>(provider => Configuration)
                .AddScoped<SmtpClient>((serviceProvider) =>
                    {
                        var config = serviceProvider.GetRequiredService<IConfiguration>();
                        return new SmtpClient()
                        {
                            Host = config.GetValue<String>("Email:Smtp:Host"),
                            Port = config.GetValue<int>("Email:Smtp:Port"),
                            Credentials = new NetworkCredential(
                                                    config.GetValue<String>("Email:Smtp:Username"),
                                                    config.GetValue<String>("Email:Smtp:Password"))
                        };
                    })                
                .AddDbContext<MasterContext>(options => options.UseSqlServer(Configuration["General:MasterDatabaseConnection"]))
                .AddDbContext<FrilivContext>(options => options.UseSqlServer(Configuration["General:FrilivDatabaseConnection"]));
            ;
            _serviceProvider = collection.BuildServiceProvider();

            _logger = _serviceProvider.GetService<ILogger<Program>>();
        }

        private static void DisposeServices()
        {
            if (_serviceProvider == null)
            {
                return;
            }
            if (_serviceProvider is IDisposable)
            {
                ((IDisposable)_serviceProvider).Dispose();
            }
        }       
    }
}