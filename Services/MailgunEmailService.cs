using AttendanceAutomation.Interfaces;
using Microsoft.Extensions.Configuration;
using RestSharp;
using RestSharp.Authenticators;

namespace AttendanceAutomation.Services
{
    public class MailgunEmailService(ILoggerService loggerService, IConfiguration configuration) : IEmailNotificationService
    {
        public void SendEmail(string subject, string message)
        {
            var apiPath = configuration["Mailgun:Endpoint"];
            var apiKey = configuration["Mailgun:ApiKey"];
            var recipient = configuration["Mailgun:Recipient"];
            var sender = configuration["Mailgun:Sender"];

            if (string.IsNullOrWhiteSpace(apiPath) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrEmpty(recipient) ||
                string.IsNullOrWhiteSpace(sender))
            {
                loggerService.Information("[MailgunEmailService]: Mailgun is not properly set up");

                return;
            }

            loggerService.Information($"[MailgunEmailService]: Sending Email...");

            var options = new RestClientOptions("https://api.mailgun.net")
            {
                Authenticator = new HttpBasicAuthenticator("api", apiKey)
            };

            var client = new RestClient(options);
            var request = new RestRequest(apiPath, Method.Post)
            {
                AlwaysMultipartFormData = true
            };
            request.AddParameter("from", sender);
            request.AddParameter("to", recipient);
            request.AddParameter("subject", subject);
            request.AddParameter("text", message);

            var response = client.ExecuteAsync(request).Result;

            if (response.IsSuccessStatusCode)
            {
                loggerService.Information($"[MailgunEmailService]: Status: {response.StatusCode}");
                loggerService.Information($"[MailgunEmailService]: Email Sent!");

                return;
            }

            loggerService.Error($"[MailgunEmailService]: Email sending failed -- {response.ErrorMessage}");
        }
    }
}
