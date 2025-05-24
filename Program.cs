using AttendanceAutomation.Interfaces;
using AttendanceAutomation.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AttendanceAutomation
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Create a service collection
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            // Build the service provider
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILoggerService>();

            try
            {
                // Resolve and run the application
                var app = serviceProvider.GetService<App>();
                app!.Run();

                logger.Information("Closing console...");
            }
            catch (Exception e)
            {
                logger.Error($"EXCEPTION: {e.Message} -- {e.StackTrace}");
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<App>();
            services.AddSingleton<IConfiguration>(provider =>
            {
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return new ConfigurationBuilder()
                    .SetBasePath(path!)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();
            });

            // Register other services
            services.AddSingleton<IEmailNotificationService, MailgunEmailService>();
            services.AddSingleton<ILoggerService, LoggerService>();
            services.AddSingleton<IConnectionService, ConnectionService>();
            services.AddSingleton<IEmaptaIntegrationService, EmaptaIntegrationService>();
        }
    }
}
