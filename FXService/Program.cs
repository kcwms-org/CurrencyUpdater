using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.IO;
using Dto;

namespace Kevcoder.FXService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var cfg = new ConfigurationBuilder()

            .AddJsonFile("appsettings.json", false, true)
            .AddJsonFile("appsettings.Development.json", true, true)
            .Build();

            Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(cfg)
            .CreateLogger();

            try
            {
                Log.Information($"Starting CurrencyConverter Service");
                CreateHostBuilder(args).Build().Run();
            }
            catch (System.Exception ex)
            {
                Log.Fatal($"error in program.cs {ex.ToString()}");
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
            .UseWindowsService()
            .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IApplicationCredentials>(s =>
                    {
                        var _cred = new ApplicationCredentials();
                        hostContext.Configuration.GetSection("xeConfiguration:ApplicationCredentials").Bind(_cred);
                        return _cred;
                    });
                    services.AddSingleton<FXCurrencyQuery>(s =>{
                        var _query = new FXCurrencyQuery();
                        hostContext.Configuration.GetSection("xeConfiguration:DefaultFXCurrencyQuery").Bind(_query);
                        return _query;
                    });
                    services.AddSingleton<Serviceconfiguration>(s =>{
                        var _svcConfig = new Serviceconfiguration();
                        hostContext.Configuration.GetSection("ServiceConfiguration").Bind(_svcConfig);
                        return _svcConfig;
                    });
                    
                    services.AddSingleton<HttpClient>();
                    services.AddHostedService<Worker>();

                }).UseSerilog();
        }
    }
}
