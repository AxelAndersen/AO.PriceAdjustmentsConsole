using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace AO.PriceAdjustments
{
    class Program
    {        
        static void Main(string[] args)
        {
            var servicesProvider = BuildDi();
            var startUp = servicesProvider.GetRequiredService<StartUp>();

            startUp.Run();

            NLog.LogManager.Shutdown();
        }

        private static ServiceProvider BuildDi()
        {
            return new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Trace);
                    builder.AddNLog(new NLogProviderOptions
                    {
                        CaptureMessageTemplates = true,
                        CaptureMessageProperties = true
                    });
                })
                .AddTransient<StartUp>()
                .BuildServiceProvider();
        }
    }
}