using System.Text.Json.Serialization;

namespace AttendanceAutomation.Models
{
    public class TokenResponseModel
    {
        [JsonPropertyName("result")]
        public ResultModel Result { get; set; }
    }

    public class ResultModel
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }
}
