using System.Text.Json.Serialization;

namespace AttendanceAutomation.Models
{
    public class AttendanceModel
    {
        [JsonPropertyName("data")]
        public AttendanceData Data { get; set; }
    }

    public class AttendanceData
    {
        [JsonPropertyName("items")]
        public List<AttendanceItem> Items { get; set; }
    }

    public class AttendanceItem
    {
        public const string COMPLETED = "Completed";
        public const string NOT_STARTED = "Not started";
        public const string ON_LEAVE = "On leave";
        public const string STARTED = "Started";
        public const string RESTDAY = "Rest Day";

        [JsonPropertyName("is_complete")]
        public bool? IsComplete { get; set; }

        [JsonPropertyName("attendance_status")]
        public string Status { get; set; }

        [JsonPropertyName("is_restday")]
        public bool? IsRestday { get; set; }
    }
}
