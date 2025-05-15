using AttendanceAutomation.Interfaces;

namespace AttendanceAutomation.Services
{
    public class ConnectionService(ILoggerService logger) : IConnectionService
    {
        public bool HasInternetConnection()
        {
            var counter = 1;
            while (counter <= 5)
            {
                logger.Information($"Checking internet connection... ({counter})");

                try
                {
                    using var httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromSeconds(5);
                    var response = httpClient.GetAsync("https://www.google.com").Result;

                    if (response.IsSuccessStatusCode)
                    {
                        logger.Information($"Successfully connected to the internet...");
                        return true;
                    }
                }
                catch
                {
                    logger.Information($"No connection will retry...");
                }

                counter++;

                Thread.Sleep(30_000);
            }

            logger.Error($"NO INTERNET CONNECTION :(");
            return false;
        }
    }
}
