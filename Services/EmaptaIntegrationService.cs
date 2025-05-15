using AttendanceAutomation.Interfaces;
using AttendanceAutomation.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
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

        private readonly string? _accessToken;
        private readonly string? _refreshToken;
        private readonly HttpClient _client;
        private readonly ILoggerService _loggerService;

        public EmaptaIntegrationService(ILoggerService loggerService, IConfiguration configuration)
        {
            _loggerService = loggerService;
            _accessToken = configuration[ACCESS_TOKEN];
            _refreshToken = configuration[REFRESH_TOKEN];

            _client = new HttpClient()
            {
                BaseAddress = new Uri("https://api.platform.emapta.com")
            };
            _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
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

            var jsonContent = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, MediaTypeNames.Application.Json);
            var result = _client.PostAsync("auth/v1/auth/protocol/openid-connect/token", jsonContent).Result;
            var response = result.Content.ReadAsStringAsync().Result;

            LogResponse(result, "RefreshToken");

            if (result.IsSuccessStatusCode)
            {
                var tokens = JsonSerializer.Deserialize<TokenResponseModel>(response);
                OverwriteAppSettings(tokens);

                return true;
            }

            return false;
        }
        public bool HasClockedIn()
        {
            var result = _client.PostAsync("time-and-attendance/ta/v1/dtr/attendance/login", new StringContent(string.Empty)).Result;
            var response = result.Content.ReadAsStringAsync().Result;

            LogResponse(result, "ClockIn");

            return result.IsSuccessStatusCode;
        }
        public bool HasClockedOut()
        {
            var result = _client.PostAsync("time-and-attendance/ta/v1/dtr/attendance/logout", new StringContent(string.Empty)).Result;
            var response = result.Content.ReadAsStringAsync().Result;

            LogResponse(result, "ClockOut");

            return result.IsSuccessStatusCode;
        }
        public bool IsShiftStarting()
        {
            return false;
        }
        public bool IsShiftCompleted()
        {
            var attendance = GetAttendanceDetails();

            if (attendance is null)
            {
                throw new Exception("API failed");
            }

            return attendance.Status == AttendanceItem.COMPLETED;
        }
        public bool IsShiftEnding()
        {
            var attendance = GetAttendanceDetails();

            if (attendance is null)
            {
                throw new Exception("API failed");
            }

            return attendance.Status == AttendanceItem.STARTED;
        }

        // Private Methods
        private AttendanceItem? GetAttendanceDetails()
        {
            var dateNow = DateTime.Now.ToString("yyyy-MM-dd");
            var path = $"time-and-attendance/ta/v1/dtr/attendance?date_from={dateNow}&date_to={dateNow}";
            var result = _client.GetAsync(path).Result;
            var response = result.Content.ReadAsStringAsync().Result;

            LogResponse(result, "AttendanceDetails");

            if (result.IsSuccessStatusCode)
            {
                var attendanceModel = JsonSerializer.Deserialize<AttendanceModel>(response);

                return attendanceModel.Data.Items.FirstOrDefault();
            }

            return null;
        }
        private void OverwriteAppSettings(TokenResponseModel? model)
        {
            try
            {
                var filePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
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
