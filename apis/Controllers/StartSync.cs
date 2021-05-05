
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
    public class StartSyncController : DriveBaseController
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {                        
            var runtime = RuntimeProvider.Get();
            var startTime = DateTime.UtcNow; 
            runtime.Log.LogInformation("Start Time: " + startTime);   
            var allFiles = await OneDrive.GetAllFiles(runtime);
            var syncInfo = await Sync.Start(runtime, allFiles, startTime);
            var elapsed = DateTime.UtcNow - startTime;
            runtime.Log.LogInformation("Duration: " + elapsed);
            return Ok(syncInfo);            
        }       
    }
}
