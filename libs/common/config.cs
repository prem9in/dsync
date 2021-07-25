namespace libs.common
{
    using System.Configuration;
    using Microsoft.Extensions.Configuration;

    public class AppConfiguration
    {
        private static object lockobj = new object();

        public static AppConfiguration Instance {get;set;}

        private readonly IConfiguration configuration;

        private AppConfiguration(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        private string Get(string key)
        {
            return this.configuration[key];
        }

        public string AzureWebJobsStorage 
        {
            get 
            {
                return this.Get("AzureWebJobsStorage");
            }
        }
        
        public string AzureWebJobsDashboard 
        {
            get 
            {
                return this.Get("AzureWebJobsDashboard");
            }
        }

        public string WEBSITE_CONTENTAZUREFILECONNECTIONSTRING 
        {
            get 
            {
                return this.Get("WEBSITE_CONTENTAZUREFILECONNECTIONSTRING");
            }
        }

        public string OAuth_BaseUri 
        {
            get 
            {
                return this.Get("OAuthBase");
            }
        }
        
        public string OAuth_ClientId 
        {
            get 
            {
                return this.Get("ClientId");
            }
        }        
        public string OAuth_ResponseType 
        {
            get 
            {
                return this.Get("ResponseType");
            }
        }
        public string OAuth_RedirectUrl 
        {
            get 
            {
                return this.Get("RedirectUrl");
            }
        }
        public string OAuth_ResponseMode 
        {
            get 
            {
                return this.Get("ResponseMode");
            }
        }
        public string OAuth_DomainHint 
        {
            get 
            {
                return this.Get("DomainHint");
            }
        }
        
        public string OAuth_Scope 
        {
            get 
            {
                return this.Get("Scope");
            }
        }
        
        public string OAuth_ClientSecret 
        {
            get 
            {
                return this.Get("ClientSecret");
            }
        }

        public string OAuth_GrantType 
        {
            get 
            {
                return this.Get("GrantType");
            }
        }

        public string OAuth_AuthorizeFormat 
        {
            get 
            {
                return this.Get("AuthorizeFormat");
            }
        }
        public string OAuth_TokenFormat 
        {
            get 
            {
                return this.Get("TokenFormat");
            }
        }
        public string OAuth_TokenPostFormat 
        {
            get 
            {
                return this.Get("TokenPostFormat");
            }
        }

        public string OAuth_RefreshTokenPostFormat 
        {
            get 
            {
                return this.Get("RefreshTokenPostFormat");
            }
        }

        public string OAuthDefaultUser 
        {
            get 
            {
                return this.Get("OAuthDefaultUser");
            }
        }     

        public string OAuth_GrantTypeRefreshToken 
        {
            get 
            {
                return this.Get("GrantTypeRefreshToken");
            }
        }     
        public string OneDriveFolderFormat 
        {
            get 
            {
                return this.Get("OneDriveFolderFormat");
            }
        }  

        public string OneDriveRootFormat 
        {
            get 
            {
                return this.Get("OneDriveRootFormat");
            }
        }       
        
        public string OneDriveBaseUri 
        {
            get 
            {
                return this.Get("OneDriveBaseUri");
            }
        } 
        public string OneDriveFileContentFormat 
        {
            get 
            {
                return this.Get("OneDriveFileContentFormat");
            }
        } 

        public string OneDriveFileThumbnailFormat 
        {
            get 
            {
                return this.Get("OneDriveFileThumbnailFormat");
            }
        }     
        
        public string BDriveStorage 
        {
            get 
            {
                return this.Get("BDriveStorage");
            }
        }

        public string CDriveStorage
        {
            get
            {
                return this.Get("CDriveStorage");
            }
        }

        public string DriveContainer 
        {
            get 
            {
                return this.Get("DriveContainer");
            }
        } 
        
        public string DriveThumbContainer 
        {
            get 
            {
                return this.Get("DriveThumbContainer");
            }
        }

        public ulong MaxAllowedSize 
        {
            get 
            {
                return ulong.Parse(this.Get("MaxAllowedSize"));
            }
        }

        public ulong SizeAfterDelete 
        {
            get 
            {
                return ulong.Parse(this.Get("SizeAfterDelete"));
            }
        }     
        public string OneDriveFileDeleteFormat 
        {
            get 
            {
                return this.Get("OneDriveFileDeleteFormat");
            }
        }        

        public static void Initialize(IConfiguration configuration)
        {
            if (Instance == null)
            {
                lock(lockobj)
                {
                    if (Instance == null)
                    {
                        Instance = new AppConfiguration(configuration);
                    }
                }
            }       
        }
    }
}