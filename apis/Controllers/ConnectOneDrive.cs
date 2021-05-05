
namespace apis.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using libs.common;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [ApiController]
    [Route("[controller]")]
    public class ConnectOneDriveController : DriveBaseController
    {
        [HttpGet]
        public HttpResponseMessage Get()
        {                        
            var runtime = RuntimeProvider.Get();
            return OAuth.RedirectToOAuth(runtime);            
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {                        
            var runtime = RuntimeProvider.Get();
            return await OAuth.CreateConnection(runtime);        
        }
    }
}
