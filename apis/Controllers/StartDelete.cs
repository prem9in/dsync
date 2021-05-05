
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
    public class StartDeleteController : DriveBaseController
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {                        
            var runtime = RuntimeProvider.Get();
            var startTime = DateTime.UtcNow;
            runtime.Log.LogInformation("Start Time: " + startTime);   
            var allFiles = await OneDrive.GetAllFiles(runtime);
            var filesToDelete = Sync.StartDelete(runtime, allFiles, startTime);
            var elapsed = DateTime.UtcNow - startTime;
            runtime.Log.LogInformation("Duration: " + elapsed);
            runtime.Log.LogInformation("To be Deleted : " + filesToDelete.Count);
            return Ok(filesToDelete.Select(f => new { f.Name, f.Type, f.FullPath, f.LastModifiedDateTime }));            
        }       
    }
}
