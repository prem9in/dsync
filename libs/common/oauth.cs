namespace libs.common
{
    using libs.models;
    using System;
    using System.Text;
    using System.Web;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using System.Net.Http.Headers;
    using System.Linq;
    using System.Collections.Specialized;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Queues;
    using Microsoft.Extensions.Logging;

    public static class OAuth
    {
        private static AuthInfo _userAuth = null;
        private static object lockObj = new object();
        public static string GetAccessoken(Runtime runtime)
        {
            if (_userAuth == null)
            {
                lock (lockObj)
                {
                    if (_userAuth == null)
                    {
                        _userAuth = runtime.AuthConnect.Query<AuthInfo>(c => c.PartitionKey == "AuthInfo" && c.RowKey == AppConfiguration.Instance.OAuthDefaultUser).FirstOrDefault();
                    }
                }  
            }

            if (_userAuth != null)
            {
                if (_userAuth.RefreshBefore < DateTime.UtcNow)
                {
                    //// we need to refresh 
                    lock (lockObj)
                    {
                        if (_userAuth.RefreshBefore < DateTime.UtcNow)
                        {
                            var postData = GetRefreshTokenPost(_userAuth.RefreshToken);
                            var tokenTask = GetToken(postData);
                            tokenTask.Wait();
                            if (tokenTask.Result.Item1 == null)
                            {
                                return string.Empty;
                            }
                            else
                            {
                                var updateTask = ProcessAuthAndSave(runtime, tokenTask.Result.Item1);
                                updateTask.Wait();
                                _userAuth = updateTask.Result;
                            }
                        }
                    }
                }

                return _userAuth.AccessToken;
            }
            
            return string.Empty;
        }

        public static HttpResponseMessage RedirectToOAuth(Runtime runtime)
        {
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = GetOAuthUrl(runtime.Log);
            return response;
        }

        public static async Task<HttpResponseMessage> CreateConnection(Runtime runtime)
        {
            var content = runtime.Request.Content;
            string postMessage = await content.ReadAsStringAsync();
            var codeResult = ParseCodeMessage(postMessage);
            if (codeResult.Item1 == null)
            {
                return CreateResponse(HttpStatusCode.BadRequest, codeResult.Item2);
            }
            else
            {
                var codeResponse = codeResult.Item1;
                var postData = GetTokenPost(codeResponse.Code);
                return await AcquireToken(runtime, postData);
            }
        }

        private static async Task<HttpResponseMessage> AcquireToken(Runtime runtime, string postData)
        {
            var tokenResult = await GetToken(postData);        
            if (tokenResult.Item1 == null)
            {
                return CreateResponse(HttpStatusCode.BadRequest, tokenResult.Item2);
            }
            else
            {
                var authInfo = await ProcessAuthAndSave(runtime, tokenResult.Item1);         
                return CreateResponse(HttpStatusCode.OK, authInfo);
            }
        }

        private static HttpResponseMessage CreateResponse<T>(HttpStatusCode code, T content)
        {
            var response = new HttpResponseMessage(code);
            response.Content = new StringContent(JsonSerializer.Serialize(content));
            return response;
        }
        
        private static async Task<AuthInfo> ProcessAuthAndSave(Runtime runtime, AuthInfo authInfo)
        {        
            authInfo.UserId = ReadUserFromJwt(authInfo.IdToken);
            if (string.IsNullOrWhiteSpace(authInfo.UserId))
            {
                authInfo.UserId = AppConfiguration.Instance.OAuthDefaultUser;
            }

            var secondsToExpiry = authInfo.Expires_In - 60 * 10; //// 10 minutes
            authInfo.RefreshBefore = DateTime.UtcNow.AddSeconds(secondsToExpiry);
            authInfo.Timestamp = DateTime.UtcNow;
            authInfo.PartitionKey = "AuthInfo";
            authInfo.RowKey = authInfo.UserId;
            authInfo.ETag = Azure.ETag.All;            
            await runtime.OnedriveConnect.UpsertEntityAsync(authInfo, TableUpdateMode.Replace);
            return authInfo;
        }


        private static string ReadUserFromJwt(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                return string.Empty;
            }

            var jwtParts = idToken.Split(new[] { '.' });
            if (jwtParts.Length == 3)
            {
                var header = jwtParts[0];
                var payload = jwtParts[1];
                var signature = jwtParts[2];
                payload = payload.Replace('-', '+'); // 62nd char of encoding
                payload = payload.Replace('_', '/'); // 63rd char of encoding
                switch (payload.Length % 4) // Pad with trailing '='s
                {
                    case 0: break; // No pad chars in this case
                    case 2: payload += "=="; break; // Two pad chars
                    case 3: payload += "="; break; // One pad char
                    default:
                        throw new System.Exception("llegal base64url string!");
                }
                byte[] buffer = Convert.FromBase64String(payload);
                var claims = Encoding.UTF8.GetString(buffer);
                var authClaims = JsonSerializer.Deserialize<OAuthClaims>(claims);
                return authClaims.PrincipalName;
            }

            return string.Empty;
        }

        private static async Task<Tuple<AuthInfo, OAuthErrorResponse>> GetToken(string data)
        {
            return await Http.MakeRequest<AuthInfo, OAuthErrorResponse>(GetOAuthTokenUrl(), HttpMethod.Post, null, data, "application/x-www-form-urlencoded");        
        }


        private static string GetTokenPost(string code)
        {
            var data = string.Format(AppConfiguration.Instance.OAuth_TokenPostFormat,
                            AppConfiguration.Instance.OAuth_ClientId,
                            AppConfiguration.Instance.OAuth_Scope,
                            AppConfiguration.Instance.OAuth_ClientSecret,
                            AppConfiguration.Instance.OAuth_GrantType,
                            code,
                            AppConfiguration.Instance.OAuth_RedirectUrl);
            return data;
        }

        private static string GetRefreshTokenPost(string refreshToken)
        {
            var data = string.Format(AppConfiguration.Instance.OAuth_RefreshTokenPostFormat,
                            AppConfiguration.Instance.OAuth_ClientId,
                            AppConfiguration.Instance.OAuth_Scope,
                            AppConfiguration.Instance.OAuth_ClientSecret,
                            AppConfiguration.Instance.OAuth_GrantTypeRefreshToken,
                            refreshToken,
                            AppConfiguration.Instance.OAuth_RedirectUrl);
            return data;
        }

        private static string GetOAuthTokenUrl()
        {
            return string.Format(AppConfiguration.Instance.OAuth_TokenFormat, AppConfiguration.Instance.OAuth_BaseUri);
        }

        private static Uri GetOAuthUrl(ILogger log)
        {
            var state = Guid.NewGuid();
            var endpoint = string.Format(AppConfiguration.Instance.OAuth_AuthorizeFormat,
                                AppConfiguration.Instance.OAuth_BaseUri,
                                AppConfiguration.Instance.OAuth_ClientId,
                                AppConfiguration.Instance.OAuth_ResponseType,
                                AppConfiguration.Instance.OAuth_RedirectUrl,
                                AppConfiguration.Instance.OAuth_ResponseMode,
                                AppConfiguration.Instance.OAuth_DomainHint,
                                AppConfiguration.Instance.OAuth_Scope,
                                Guid.NewGuid());
            log.LogInformation("Outh URL generated with state : " + state.ToString("D"));
            return new Uri(endpoint);
        }

        private static Tuple<OAuthCodeResponse, OAuthErrorResponse> ParseCodeMessage(string message)
        {
            var dataCollection = HttpUtility.ParseQueryString(message);
            if (string.IsNullOrWhiteSpace(dataCollection["error"]))
            {
                return Tuple.Create<OAuthCodeResponse, OAuthErrorResponse>(new OAuthCodeResponse(dataCollection), null);
            }
            else
            {
                return Tuple.Create<OAuthCodeResponse, OAuthErrorResponse>(null, new OAuthErrorResponse(dataCollection));
            }
        }
    
    }
}