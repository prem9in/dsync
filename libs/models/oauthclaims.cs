namespace libs.models
{    
    using Azure.Data.Tables;    
    using System.Text.Json.Serialization;
    
    public class OAuthClaims : TableEntityBase 
    {
        [JsonPropertyName("aud")]
        public string Audience { get; set; }
        [JsonPropertyName("exp")]
        public int Expiration { get; set; }
        [JsonPropertyName("iss")]
        public string Issuer { get; set; }
        [JsonPropertyName("iat")]
        public int IssuedAtTime { get; set; }
        [JsonPropertyName("nbf")]
        public int NotBeforeTime { get; set; }
        [JsonPropertyName("oid")]
        public string ObjectIdentifier { get; set; }
        [JsonPropertyName("tid")]
        public string TenantIdentifier { get; set; }
        [JsonPropertyName("sub")]
        public string SubjectIdentifier { get; set; }
        [JsonPropertyName("upn")]
        public string PrincipalName { get; set; }
        [JsonPropertyName("unique_name")]
        public string UniqueName { get; set; }
        [JsonPropertyName("ver")]
        public string Version { get; set; }

        public override TableEntity ToTableEntity()
        {            
            var tentity = base.ToTableEntity();
            tentity.Add("Audience", this.Audience);
            tentity.Add("Expiration", this.Expiration);
            tentity.Add("Issuer", this.Issuer);
            tentity.Add("IssuedAtTime", this.IssuedAtTime);
            tentity.Add("NotBeforeTime", this.NotBeforeTime);
            tentity.Add("ObjectIdentifier", this.ObjectIdentifier);
            tentity.Add("TenantIdentifier", this.TenantIdentifier);
            tentity.Add("SubjectIdentifier", this.SubjectIdentifier);
            tentity.Add("PrincipalName", this.PrincipalName);
            tentity.Add("UniqueName", this.UniqueName);
            tentity.Add("Version", this.Version);
            return tentity;
        }
    }
}