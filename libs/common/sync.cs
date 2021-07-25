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
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using Azure.Data.Tables;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Queues;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;

    public static class Sync
    {
        private static List<string> SafeFolders = new List<string>() { "/Study/", "/Vehicle/", "/Sushma/", "/Personal/", "Docs", "/BellaBaby/" };
        public static  List<OneDriveItem> StartDelete(Runtime runtime, IEnumerable<OneDriveItem> allDriveFiles, DateTime timeStamp)
        {
            ulong driveSize = 0;
            foreach(var files in allDriveFiles)
            {
                driveSize += (ulong)files.Size;
            }
            runtime.Log.LogInformation("Total drive size " + driveSize);
            var filesToDelete = new List<OneDriveItem>();
            if (driveSize > AppConfiguration.Instance.MaxAllowedSize)
            {
                var newDriveSize = driveSize;
                runtime.Log.LogInformation("Getting Photo files");
                var pFiles = runtime.PhotoInfoMeta.Query<PhotoInfo>(c => c.PartitionKey == "PhotoFiles").ToList();
                var photoFiles = pFiles.Where(c => c.TakenDateTime != default(DateTimeOffset));
                runtime.Log.LogInformation("Photo files count : " + photoFiles.Count());
                runtime.Log.LogInformation("Setting last modified date by photo taken date");
                foreach (var exFiles in allDriveFiles)
                {
                    var photo = photoFiles.FirstOrDefault(p => p.Id == exFiles.Id);
                    if (photo != null)
                    {
                        exFiles.LastModifiedDateTime = photo.TakenDateTime;
                    }
                }

                runtime.Log.LogInformation("Ordering by LastModifiedTime");
                var orderedFiles = allDriveFiles.OrderBy(f => f.LastModifiedDateTime);
                runtime.Log.LogInformation("Getting Existing blobed files");
                var existingFiles = runtime.FileInfoMeta.Query<FileInfo>(c => c.PartitionKey == "DriveFiles" && c.Blobed).ToList();
                runtime.Log.LogInformation("Existing files count " + existingFiles.Count);
                foreach (var files in orderedFiles)
                {
                    if (SafeFolders.Any(s => files.FullPath.IndexOf(s, StringComparison.OrdinalIgnoreCase) > -1)
                        || files.FullPath.LastIndexOf("/") <= 1)
                    {
                        runtime.Log.LogInformation("Safe file " + files.FullPath);
                        continue;
                    }

                    //// if files is present in blobed files list then its okay to delete
                    if (existingFiles.Any(e => e.Id == files.Id))
                    {
                        filesToDelete.Add(files);
                        var mdata = JsonSerializer.Serialize(files);
                        runtime.DeleteQueue.SendMessageAsync(mdata);
                        newDriveSize = newDriveSize - (ulong)files.Size;
                        if (newDriveSize < AppConfiguration.Instance.SizeAfterDelete)
                        {
                            // runtime.DeleteQueue.AddAsync(files);
                    
                            /// we have reached the size to stop delete.
                            runtime.Log.LogInformation("Expected drive size after deleting  " + newDriveSize);
                            break;
                        }
                    }
                    else
                    {
                        runtime.Log.LogInformation("Skip non-blobed file : " + files.FullPath);
                    }
                }
            }

            runtime.Log.LogInformation("Number of files to delete  " + filesToDelete.Count);
            if (filesToDelete.Count > 0)
            {
                var last = filesToDelete[filesToDelete.Count - 1];
                runtime.Log.LogInformation("Last file to deleted  " + last.Type + " " + last.FullPath + " " + last.LastModifiedDateTime);
            } 
            
            return filesToDelete;
        }

        public static async Task<bool> SyncFile(Runtime runtime, FileInfo filetoSync)
        {
            var taskList = new List<Task>();
            runtime.Log.LogInformation("Getting content and Thumbnail for " + filetoSync.ToString());
            var fileTask = OneDrive.GetFileContent(runtime, filetoSync.Id, filetoSync.Size);
            var thumbTask = OneDrive.GetThumbnail(runtime, filetoSync.Id, filetoSync.Size);
            taskList.Add(fileTask);
            taskList.Add(thumbTask);
            await Task.WhenAll(taskList);
            taskList.Clear();

            Task<FileInfo> uploadFile = null;
            Task<FileInfo> uploadThumb = null;
            if (fileTask.Result == null)
            {
                runtime.Log.LogWarning("No Content available for " + filetoSync.ToString());
            }
            else
            {
                runtime.Log.LogInformation("Uploading Content for " + filetoSync.ToString());
                uploadFile = BlobDrive.Upload(runtime, filetoSync, false, fileTask.Result);
            }

            if (thumbTask.Result == null)
            {
                runtime.Log.LogWarning("No Thumbnail available for " + filetoSync.ToString());
            }
            else
            {
                runtime.Log.LogInformation("Uploading Thumbnail for " + filetoSync.ToString());
                uploadThumb = BlobDrive.Upload(runtime, filetoSync, true, thumbTask.Result);
            }
        
            if (uploadFile != null)
            {
                taskList.Add(uploadFile);
            }

            if (uploadThumb != null)
            {
                taskList.Add(uploadThumb);
            }
        
            await Task.WhenAll(taskList);
            if (uploadFile != null && uploadFile.Result.Blobed)
            {
                runtime.Log.LogInformation("Marking Blobed " + filetoSync.ToString());                
                await runtime.FileInfoTable.UpsertEntityAsync(filetoSync, TableUpdateMode.Replace);
            }
            else
            {
                runtime.Log.LogWarning("Marking Blobed Failed for " + filetoSync.ToString());
            }

            return true;
        }

        public static async Task<SyncInfo> Start(Runtime runtime,  IEnumerable<OneDriveItem> allDriveFiles, DateTime timeStamp)
        {
            var syncId = Guid.NewGuid();
            runtime.Log.LogInformation("Sync started with id : " + syncId.ToString("D"));
            var allFiles = new List<FileInfo>();
            foreach (var d in allDriveFiles)
            {
                if (!d.IsDeleted)
                {
                    allFiles.Add(new FileInfo(syncId, d, timeStamp));
                }
            }
            
            var newFiles = GetNewFiles(runtime, allFiles);       
            var newFileCount = 0;
            long totalSize = 0;
            if (newFiles.Any())
            {   
                var errorList = new List<string>();                        
                var duplicates = new Dictionary<string, FileInfo>();
                
                foreach (var nfile in newFiles)
                {                    
                    if (duplicates.ContainsKey(nfile.Id))
                    {
                        runtime.Log.LogError("Duplicate: New Record : " + nfile.ToString());
                        var erec = duplicates[nfile.Id];
                        runtime.Log.LogError("Duplicate: Existing Record : " + erec.ToString());
                    }
                    else
                    {
                        newFileCount++;
                        duplicates.Add(nfile.Id, nfile);
                        totalSize = totalSize + nfile.Size;
                        var dfile = allDriveFiles.FirstOrDefault(a => a.Id == nfile.Id);
                        if (dfile == null)
                        {
                            var err = "Id not found in alldrivefiles: " + nfile.ToString();
                            errorList.Add(err);   
                        }
                        else
                        {
                            var fresult = await runtime.FileInfoTable.UpsertEntityAsync(nfile, TableUpdateMode.Replace);
                            if (fresult != null && fresult.Status != 200)
                            {
                                var err = "Fail to update in Table: " + nfile.ToString();
                                errorList.Add(err); 
                            }

                            Azure.Response dresult = null;
                            switch (dfile.Type)
                            {
                                case FileType.Photo:                                    
                                    var pdata = new PhotoInfo(syncId, dfile, timeStamp);
                                    dresult = await runtime.FileInfoTable.UpsertEntityAsync(pdata, TableUpdateMode.Replace);
                                    if (dresult != null && dresult.Status != 200)
                                    {
                                        var err = "Fail to update in Table: " + pdata.ToString();
                                        errorList.Add(err); 
                                    }
                                    break;
                                case FileType.Audio:                                    
                                    var adata = new AudioInfo(syncId, dfile, timeStamp);                                   
                                    dresult = await runtime.FileInfoTable.UpsertEntityAsync(adata, TableUpdateMode.Replace);
                                    if (dresult != null && dresult.Status != 200)
                                    {
                                        var err = "Fail to update in Table: " + adata.ToString();
                                        errorList.Add(err); 
                                    }
                                    break;
                                case FileType.Video:                                    
                                    var vdata = new VideoInfo(syncId, dfile, timeStamp);   
                                    dresult = await runtime.FileInfoTable.UpsertEntityAsync(vdata, TableUpdateMode.Replace);    
                                    if (dresult != null && dresult.Status != 200)
                                    {
                                        var err = "Fail to update in Table: " + vdata.ToString();
                                        errorList.Add(err); 
                                    }                             
                                    break;
                            }                            

                            if (dfile.Location != null)
                            {                                
                                var ldata = new LocationInfo(syncId, dfile, timeStamp);
                                var lresult = await runtime.FileInfoTable.UpsertEntityAsync(ldata, TableUpdateMode.Replace);  
                                if (lresult != null && lresult.Status != 200)
                                {
                                    var err = "Fail to update in Table: " + ldata.ToString();
                                    errorList.Add(err); 
                                }                              
                            }                                                                                 

                            runtime.Log.LogInformation("Saved " + newFileCount + " items to DriveFiles.");
                            
                            var fdata = JsonSerializer.Serialize(nfile);                          
                            await runtime.OutputQueue.SendMessageAsync(fdata);                          
                        }
                    }
                }

                if (errorList.Count > 0)
                {
                    foreach (var eitem in errorList)
                    {
                        runtime.Log.LogWarning(eitem);
                    }

                    runtime.Log.LogWarning("Total Errored items : " + errorList.Count.ToString());
                }
            
                runtime.Log.LogInformation("All files synced to DriveFiles.");         
            }

            runtime.Log.LogInformation("Done with sync.. updating syncinfo");
            var elapsed = DateTime.UtcNow - timeStamp;
            runtime.Log.LogInformation("Total files : " + newFileCount);
            runtime.Log.LogInformation("Total size : " + totalSize);
            runtime.Log.LogInformation("Sync duration : " + elapsed);
            var syncInfo = new SyncInfo
            {            
                Duration = elapsed.TotalSeconds,
                Count = newFileCount,
                Size = totalSize,
                Timestamp = timeStamp,
                PartitionKey = "SyncInfo",            
                RowKey = syncId.ToString("D"),
                ETag = Azure.ETag.All
            };
                    
            await runtime.syncInfoTable.UpsertEntityAsync(syncInfo);
            runtime.Log.LogInformation("Syncinfo updated");
            return syncInfo;
        }         

        private static IEnumerable<FileInfo> GetNewFiles(Runtime runtime, IEnumerable<FileInfo> allFiles)
        {
            runtime.Log.LogInformation("All files Count : " + allFiles.Count());
            var existingFiles = runtime.FileInfoMeta.Query<FileInfo>(c => c.PartitionKey == "DriveFiles").ToList();
            runtime.Log.LogInformation("Existing files Count : " + existingFiles.Count);
            var newFiles = allFiles.Except(existingFiles, new FileInfoComparer()).ToList();
            runtime.Log.LogInformation("Except files Count : " + newFiles.Count);
            var nonBlobedFiles = existingFiles.Where(b => !b.Blobed);
            runtime.Log.LogInformation("Non blobed files Count : " + nonBlobedFiles.Count());
            newFiles.AddRange(nonBlobedFiles);
            runtime.Log.LogInformation("Final New file Count : " + newFiles.Count);
            return newFiles;
        }

    }
}