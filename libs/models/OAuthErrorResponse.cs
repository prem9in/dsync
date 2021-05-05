namespace libs.models
{
    using System.Text.Json.Serialization;
    using System.Collections.Specialized;

    public class OAuthErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }
        [JsonPropertyName("error_description")]
        public string ErrorDescription { get; set; }
        [JsonPropertyName("correlation_id")]
        public string State { get; set; }

        public OAuthErrorResponse(NameValueCollection item)
        {
            this.Error = item["error"];
            this.ErrorDescription = item["error_description"];
            this.State = item["state"];        
        }
    }
}