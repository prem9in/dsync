namespace libs.models
{       
    using System.Text.Json.Serialization;
    
    public class OneDriveError
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("message")]
        public string message { get; set; }

        [JsonPropertyName("innererror")]
        public OneDriveError InnerError { get; set; }
    }

    public class OneDriveErrorResponse
    {
        [JsonPropertyName("error")]
        public OneDriveError Error { get; set; }
    }
}