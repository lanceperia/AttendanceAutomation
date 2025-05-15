using System.Text.Json.Serialization;

namespace AttendanceAutomation.Models
{
    public class EmailModel
    {
        [JsonPropertyName("from")]
        public RecipientModel From { get; set; }

        [JsonPropertyName("to")]
        public List<RecipientModel> To { get; set; }

        [JsonPropertyName("template_id")]
        public string TemplateId { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; }

        [JsonPropertyName("personalization")]
        public List<Personalization> Personalization { get; set; }
    }

    public class RecipientModel
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class Personalization
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("body")]
        public string Body { get; set; }
    }
}
