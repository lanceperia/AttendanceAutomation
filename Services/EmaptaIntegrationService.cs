using AttendanceAutomation.Interfaces;
using AttendanceAutomation.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Reflection;
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

            var _client = PrepareClient();
            var jsonContent = new StringContent(JsonSerializer.Serialize(dto), 
                Encoding.UTF8, 
                MediaTypeNames.Application.Json);
            var result = _client.PostAsync("auth/v1/auth/protocol/openid-connect/token", jsonContent).Result;
            var response = result.Content.ReadAsStringAsync().Result;

            LogResponse(result, "RefreshToken");

            if (result.IsSuccessStatusCode)
            {
                var tokens = JsonSerializer.Deserialize<TokenResponseModel>(response);
                
                // Overwrite AppSettings in published build
                OverwriteAppSettings(tokens, _appSettingsPath);

                _accessToken = tokens!.Result.AccessToken;

                return true;
            }

            return false;
        }

        public bool HasClockedIn()
        {
            return IsAttendanceActionSuccessful("time-and-attendance/ta/v1/dtr/attendance/login", "ClockIn");
        }
        public bool HasClockedOut()
        {
            return IsAttendanceActionSuccessful("time-and-attendance/ta/v1/dtr/attendance/logout", "ClockOut");
        }
        public AttendanceItem GetAttendanceDetails()
        {
            var dateNow = DateTime.Now.ToString("yyyy-MM-dd");
            var path = $"time-and-attendance/ta/v1/dtr/attendance?date_from={dateNow}&date_to={dateNow}";

            var _client = PrepareClient();
            var result = _client.GetAsync(path).Result;
            var response = result.Content.ReadAsStringAsync().Result;

            LogResponse(result, "AttendanceDetails");

            if (result.IsSuccessStatusCode)
            {
                var attendanceModel = JsonSerializer.Deserialize<AttendanceModel>(response);

                return attendanceModel.Data.Items.FirstOrDefault();
            }

            throw new Exception("API failed");

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
            var _client = PrepareClient();
            var result = _client.PostAsync(apiPath, new StringContent(string.Empty)).Result;
            var response = result.Content.ReadAsStringAsync().Result;

            LogResponse(result, action);

            return result.IsSuccessStatusCode;
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
        private void LogResponse(HttpResponseMessage? message, string method)
        {
            try
            {
                message!.EnsureSuccessStatusCode();

                _loggerService.Information($"({method}) {message.StatusCode}: Success");
            }
            catch (Exception e)
            {
                _loggerService.Error($"{message!.StatusCode}: {message.Content} -- {e.StackTrace}");
            }
        }
    }
}
