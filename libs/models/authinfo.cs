namespace libs.models
{
    using Azure.Data.Tables;
    using System;    
    using System.Text.Json.Serialization;    

    public class AuthInfo : TableEntityBase 
    {
        [JsonPropertyName("userid")]
        public string UserId { get; set; }

        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int Expires_In 
        { 
            get
            {
                int newValue = 0;
                Int32.TryParse(ExpiresIn, out newValue);
                return newValue;
            }
            set
            {
                this.ExpiresIn = value.ToString();
            }
        }

        public string ExpiresIn { get; set; }

        [JsonPropertyName("expires_on")]
        public string ExpiresOn { get; set; }

        [JsonPropertyName("refresh_before")]
        public DateTime RefreshBefore { get; set; }

        [JsonPropertyName("resource")]
        public string Resource { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("scope")]
        public string Scope { get; set; }

        [JsonPropertyName("id_token")]
        public string IdToken { get; set; }

        public override TableEntity ToTableEntity()
        {
            var tentity = base.ToTableEntity();
            tentity.Add("UserId", this.UserId);
            tentity.Add("AccessToken", this.AccessToken);
            tentity.Add("TokenType", this.TokenType);
            tentity.Add("ExpiresIn", this.ExpiresIn);
            tentity.Add("ExpiresOn", this.ExpiresOn);
            tentity.Add("RefreshBefore", this.RefreshBefore);
            tentity.Add("Resource", this.Resource);
            tentity.Add("RefreshToken", this.RefreshToken);
            tentity.Add("Scope", this.Scope);
            tentity.Add("IdToken", this.IdToken);
            return tentity;
        }
    }
}