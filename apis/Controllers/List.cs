
namespace apis.Controllers
{
    using System;
    using System.Web;
    using System.Net;
    using System.Net.Http;
    using libs.common;
    using libs.models;
    using System.Collections.Generic;
    using System.Collections.Concurrent;
    using System.Linq;    
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    [ApiController]
    [Route("[controller]")]
    public class ListController : DriveBaseController
    {
        private static CacheResult CachedResult;

        [HttpGet]
        public async Task<IActionResult> Get()
        {                        
            var runtime = RuntimeProvider.Get();            
            var skip = Request.Query["skip"].ToString();
            var top = Request.Query["top"].ToString();
            var orderBy = Request.Query["orderby"].ToString();
            var refresh = Request.Query["refresh"].ToString();
            var start = DateTime.UtcNow;
            runtime.Log.LogInformation("Start : " + start);

            if (CachedResult == null || (!string.IsNullOrWhiteSpace(refresh) && refresh.Equals("true", StringComparison.OrdinalIgnoreCase))) 
            {                               
                var taskLists = new List<Task>();
                var existingFiles = Task.Run(
                        () => Extensions.RunWithInstrumentation(
                            ()=> runtime.FileInfoTable.Query<FileInfo>(
                                c => c.PartitionKey == "DriveFiles" && c.Blobed).Select(f => new { f.Id, f.Extension, f.FullPath, f.MimeType, f.LastModifiedBy, f.LastModified, f.Size, f.Type, f.Name }), "FileQuery", runtime.Log));       
                taskLists.Add(existingFiles);

                var pFiles = Task.Run(
                    () => Extensions.RunWithInstrumentation(
                        ()=> runtime.FileInfoTable.Query<PhotoInfo>(
                            c => c.PartitionKey == "PhotoFiles").Select(f => new { f.Id, f.CameraMake, f.CameraModel, f.FNumber, f.FocalLength, f.Height, f.Iso, f.TakenDateTime, f.Width }), "PhotoQuery", runtime.Log));               
                taskLists.Add(pFiles);

                var vFiles = Task.Run(
                    () => Extensions.RunWithInstrumentation(
                        ()=> runtime.FileInfoTable.Query<VideoInfo>(
                            c => c.PartitionKey == "VideoFiles").Select(f => new { f.Id, f.Height, f.Duration, f.Width }), "VideoQuery", runtime.Log));
                taskLists.Add(vFiles);
                
                var thumbToken = Task.Run(
                    () => Extensions.RunWithInstrumentation(
                        () => BlobDrive.GetKey(runtime, true), "ThumbToken", runtime.Log));
                taskLists.Add(thumbToken);

                var driveToken = Task.Run(
                    () => Extensions.RunWithInstrumentation(
                        () => BlobDrive.GetKey(runtime, false), "DriveToken", runtime.Log));
                taskLists.Add(driveToken);

                var thumbUri = Task.Run(
                    () => Extensions.RunWithInstrumentation(
                        () => BlobDrive.GetUri(runtime, true), "ThumbUri", runtime.Log));
                taskLists.Add(thumbUri);

                var driveUri = Task.Run(
                    () => Extensions.RunWithInstrumentation(
                        () => BlobDrive.GetUri(runtime, false), "DriveUri", runtime.Log));
                taskLists.Add(driveUri);
                
                await Task.WhenAll(taskLists);
                
                var processTaskLists = new List<Task>();
                var orderTask = Task.Run(
                    () => Extensions.RunWithInstrumentation(
                        () => existingFiles.Result.ToList().OrderByDescending(o => o.LastModified), "FileList_Execute", runtime.Log));
                processTaskLists.Add(orderTask);

                var photoTask = Task.Run(
                    () => Extensions.RunWithInstrumentation(
                        () => pFiles.Result.ToDictionary(p => p.Id, p => (object)p), "PhotoFiles_Execute", runtime.Log));
                processTaskLists.Add(photoTask);

                var videoTask = Task.Run(
                    () => Extensions.RunWithInstrumentation(
                        () => vFiles.Result.ToDictionary(v => v.Id, v => (object)v), "VideoFiles_Execute", runtime.Log));
                processTaskLists.Add(videoTask);

                await Task.WhenAll(processTaskLists);
                //// collect the results    
                IEnumerable<dynamic> orderedFiles = orderTask.Result;
                var allPhotofiles = photoTask.Result;
                var allVideofiles = videoTask.Result;

                CachedResult = new CacheResult();
                CachedResult.AllFiles = orderedFiles;
                CachedResult.AllPhotofiles = allPhotofiles;
                CachedResult.AllVideofiles = allVideofiles;
                CachedResult.ThumbToken = thumbToken.Result;
                CachedResult.ThumbUri = thumbUri.Result;
                CachedResult.DriveToken = driveToken.Result;
                CachedResult.DriveUri = driveUri.Result;
                CachedResult.TotalCount = orderedFiles.Count();
            }
        
            // collect the results    
            IEnumerable<dynamic> corderedFiles = CachedResult.AllFiles;
            var callPhotofiles = CachedResult.AllPhotofiles;
            var callVideofiles = CachedResult.AllVideofiles;

            if (!string.IsNullOrWhiteSpace(orderBy))
            {
                if (orderBy.EndsWith(" desc", StringComparison.OrdinalIgnoreCase))
                {
                    // orderedFiles = existingFiles.Result.OrderByDescending(orderBy);
                }
                else
                {
                //  orderedFiles = existingFiles.Result.OrderBy(orderBy);
                }
            
            }
            int skipValue = 0;
            if (!string.IsNullOrWhiteSpace(skip) && Int32.TryParse(skip, out skipValue))
            {
                corderedFiles = corderedFiles.Skip(skipValue);
            }
            int topValue = 0;
            if (!string.IsNullOrWhiteSpace(top) && Int32.TryParse(top, out topValue))
            {
                corderedFiles = corderedFiles.Take(topValue);
            }

            var selectedPhotofiles = new ConcurrentBag<dynamic>();
            var selectedVideofiles = new ConcurrentBag<dynamic>();
            Parallel.ForEach(corderedFiles, (of) => {
                if (of.Type == "Photo" && callPhotofiles.ContainsKey(of.Id))
                {
                    selectedPhotofiles.Add(callPhotofiles[of.Id]);            
                }

                if (of.Type == "Video" && callVideofiles.ContainsKey(of.Id))
                {           
                    selectedVideofiles.Add(callVideofiles[of.Id]);            
                } 
            });   
        
            var elapsed = DateTime.UtcNow - start;
            runtime.Log.LogInformation("Total Duration : " + elapsed);
            var result = new { 
                        Duration = elapsed, 
                        Url = CachedResult.DriveUri, 
                        ThumbUrl = CachedResult.ThumbUri, 
                        DriveToken =  CachedResult.DriveToken, 
                        ThumbToken = CachedResult.ThumbToken, 
                        Files = corderedFiles, 
                        Photos = selectedPhotofiles, 
                        Videos = selectedVideofiles,
                        TotalCount = CachedResult.TotalCount
                    };   

            return Ok(result);         
        }       
    }
}
