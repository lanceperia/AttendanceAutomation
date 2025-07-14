using AttendanceAutomation.Interfaces;
using AttendanceAutomation.Models;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AttendanceAutomation.Services
{
    public class EmaptaIntegrationService : IEmaptaIntegrationService
    {
        private const string ACCESS_TOKEN = "AccessToken";
        private const string REFRESH_TOKEN = "RefreshToken";

        private readonly HttpClient _client;
        private readonly ILoggerService _loggerService;
        private readonly string? _appSettingsPath;
        private string? _refreshToken;
        private string? _accessToken;

        public EmaptaIntegrationService(ILoggerService loggerService, IConfiguration configuration)
        {
            _loggerService = loggerService;
            _appSettingsPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

            FetchAppSettings();
        }

        public bool HasToken()
        {
            return !string.IsNullOrWhiteSpace(_accessToken)
                && !string.IsNullOrWhiteSpace(_refreshToken);
        }
        public bool IsTokenRefreshed()
        {
            var dto = new TokenRequestModel()
            {
                ClientId = "EMAPTA-MYEMAPTAWEB",
                GrantType = "refresh_token",
                RefreshToken = _refreshToken,
                Scope = "openid"
            };

            return ExecuteWithRetry(client =>
            {
                var jsonContent = new StringContent(JsonSerializer.Serialize(dto),
                    Encoding.UTF8,
                    MediaTypeNames.Application.Json);
                var result = client.PostAsync("auth/v1/auth/protocol/openid-connect/token", jsonContent).Result;
                var response = result.Content.ReadAsStringAsync().Result;

                if (result.IsSuccessStatusCode)
                {
                    var tokens = JsonSerializer.Deserialize<TokenResponseModel>(response);

                    // Overwrite AppSettings in published build
                    OverwriteAppSettings(tokens, _appSettingsPath);

                    _accessToken = tokens!.Result.AccessToken;
                }

                return result?.IsSuccessStatusCode ?? null;

            }, nameof(IsTokenRefreshed), 5, 1000) ?? false;
        }

        public bool HasClockedIn()
        {
            return IsAttendanceActionSuccessful("time-and-attendance/ta/v1/dtr/attendance/login", "ClockIn");
        }
        public bool HasClockedOut()
        {
            return IsAttendanceActionSuccessful("time-and-attendance/ta/v1/dtr/attendance/logout", "ClockOut");
        }
        public AttendanceItem? GetAttendanceDetails()
        {
            return ExecuteWithRetry(client =>
            {
                var dateNow = DateTime.Now.ToString("yyyy-MM-dd");
                var path = $"time-and-attendance/ta/v1/dtr/attendance?date_from={dateNow}&date_to={dateNow}";
                var result = client.GetAsync(path).Result;
                var response = result.Content.ReadAsStringAsync().Result;

                LogResponse(response, "AttendanceDetails", result.StatusCode);

                if (!result.IsSuccessStatusCode)
                {
                    return null;
                }

                var attendanceModel = JsonSerializer.Deserialize<AttendanceModel>(response);
                return attendanceModel.Data.Items.FirstOrDefault();
            }, nameof(GetAttendanceDetails));
        }

        // Private Methods
        private HttpClient PrepareClient()
        {
            var _client = new HttpClient()
            {
                BaseAddress = new Uri("https://api.platform.emapta.com")
            };
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");

            return _client;
        }
        private bool IsAttendanceActionSuccessful(string apiPath, string action)
        {
            return ExecuteWithRetry(client =>
            {
                var result = client.PostAsync(apiPath, new StringContent(string.Empty)).Result;
                var response = result.Content.ReadAsStringAsync().Result;

                LogResponse(response, action, result.StatusCode);

                return result?.IsSuccessStatusCode ?? null;

            }, nameof(IsAttendanceActionSuccessful)) ?? false;
        }

        private void OverwriteAppSettings(TokenResponseModel? model, string filePath)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var root = JsonNode.Parse(json);

                if (root is JsonObject jObject)
                {
                    jObject[ACCESS_TOKEN] = model.Result.AccessToken;
                    jObject[REFRESH_TOKEN] = model.Result.RefreshToken;

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(filePath, jObject.ToJsonString(options));
                }
            }
            catch (Exception e)
            {
                _loggerService.Error($"{e.Message} -- {e.StackTrace}");
            }

        }
        private void FetchAppSettings()
        {
            try
            {
                var json = File.ReadAllText(_appSettingsPath);
                var root = JsonNode.Parse(json);

                if (root is JsonObject jObject)
                {
                    _accessToken = jObject[ACCESS_TOKEN].ToString();
                    _refreshToken = jObject[REFRESH_TOKEN].ToString();

                    return;
                }

                _loggerService.Error($"Can't read file");
            }
            catch (Exception e)
            {
                _loggerService.Error($"{e.Message} -- {e.StackTrace}");
            }
        }
        private void LogResponse(string message, string method, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var httpStatusCodes = new List<HttpStatusCode> { HttpStatusCode.OK, HttpStatusCode.Found, HttpStatusCode.Created };

            if (httpStatusCodes.Contains(statusCode))
            {
                _loggerService.Information(message);
                return;
            }

            _loggerService.Error($"{statusCode}: {message}");
        }
        private T? ExecuteWithRetry<T>(Func<HttpClient, T?> action, string functionName, int retries = 3, int delayMs = 3000)
        {
            while (retries > 0)
            {
                try
                {
                    using var client = PrepareClient();
                    var result = action(client);
                    if (result != null)
                    {
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    _loggerService.Error($"Exception: {ex.Message}");
                }

                retries--;
                if (retries > 0)
                {
                    LogResponse($"Retrying ({retries})", functionName);

                    Thread.Sleep(delayMs);
                }
            }

            return default;
        }
    }
}
