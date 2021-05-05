namespace libs.common
{
    using System;
    using System.Threading.Tasks;
    using Azure.Data.Tables;
    using Azure.Storage.Sas;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Queues;
    using libs.models;
    using System.Collections.Generic;
    using Azure;
    using Microsoft.Extensions.Logging;


    public static class BlobDrive
    {
        private static BlobContainerClient driveContainer = null;
        private static BlobContainerClient driveThumbContainer = null;
        private static object lockobj = new object();

        private static BlobUploadOptions upoptions = new BlobUploadOptions {
            TransferOptions = new Azure.Storage.StorageTransferOptions {
                MaximumConcurrency = 10
            }
        };

        public static string GetKey(Runtime runtime, bool isThumbnail)
        {
            Initialize();
            var container = isThumbnail ? driveThumbContainer : driveContainer;                      
            var sasuri = container.GenerateSasUri(BlobContainerSasPermissions.Read, DateTimeOffset.UtcNow.AddDays(1));
            return sasuri.Query;
        }

        public static string GetUri(Runtime runtime, bool isThumbnail)
        {
            Initialize();
            var container = isThumbnail ? driveThumbContainer : driveContainer;       
            return container.Uri.AbsoluteUri;
        }

        public static async Task<List<BlobItem>> List(Runtime runtime, bool useFlatBlobListing)
        {
            Initialize();
            var results = new List<BlobItem>();
            if (useFlatBlobListing)
            {
                var resultSegment = driveContainer.GetBlobsAsync().AsPages();
                await foreach (Azure.Page<BlobItem> blobPage in resultSegment)
                {
                    foreach (BlobItem blobItem in blobPage.Values)
                    {
                        Console.WriteLine("Blob name: {0}", blobItem.Name);
                        results.Add(blobItem);
                    }

                    Console.WriteLine();
                }
            }
            else 
            {
                var resultSegment = driveContainer.GetBlobsByHierarchyAsync().AsPages();                

                // Enumerate the blobs returned for each page.
                await foreach (Azure.Page<BlobHierarchyItem> blobPage in resultSegment)
                {
                    // A hierarchical listing may return both virtual directories and blobs.
                    foreach (BlobHierarchyItem blobhierarchyItem in blobPage.Values)
                    {
                        if (!blobhierarchyItem.IsPrefix)
                        {                      
                            // Write out the name of the blob.
                            Console.WriteLine("Blob name: {0}", blobhierarchyItem.Blob.Name);
                            results.Add(blobhierarchyItem.Blob);
                        }
                    }

                    Console.WriteLine();
                }
            }

            return results;
        }

        public static async Task<FileInfo> Upload(Runtime runtime, FileInfo filemeta, bool isThumbnail, System.IO.Stream fstream)
        {
            Initialize();
            var blobName = NormalizeBlobName(filemeta.FullPath);
            runtime.Log.LogInformation("Uploading to Blob for " + filemeta.ToString());
            var blref = isThumbnail ? driveThumbContainer.GetBlobClient(blobName) :
                        driveContainer.GetBlobClient(blobName);
            try
            {
                await blref.UploadAsync(fstream, upoptions);
                runtime.Log.LogInformation("Setting Metadata for " + filemeta.ToString());
                var mdata = new Dictionary<string, string>();
                mdata.Add("SyncId", filemeta.SyncId.ToString("D"));
                mdata.Add("Id", filemeta.Id);
                mdata.Add("Type", filemeta.Type);
                mdata.Add("MimeType", filemeta.MimeType);
                mdata.Add("Size", filemeta.Size.ToString());
                await blref.SetMetadataAsync(mdata);
                runtime.Log.LogInformation("Setting Properties for " + filemeta.ToString());
                filemeta.Blobed = true;
                return filemeta;
            }
            catch (RequestFailedException)
            {
                runtime.Log.LogWarning("Error Detected while uploading " + filemeta);
                await blref.UploadAsync(string.Empty, upoptions);
                runtime.Log.LogInformation("Uploading a zero byte blob block");
                runtime.Log.LogInformation("Sleep 5 seconds");
                System.Threading.Thread.Sleep(1000 * 5);
                runtime.Log.LogInformation("Delete bad blob block");
                blref.DeleteIfExists();
                runtime.Log.LogInformation("Success ... bad blob block removed.");
                runtime.Log.LogInformation("Sleep 5 seconds");
                System.Threading.Thread.Sleep(1000 * 5);
                throw;
            }
            
        }

        private static string NormalizeBlobName(string blobName)
        {
            if (blobName.StartsWith("/"))
            {
                blobName = blobName.Remove(0, 1);
            }

            return blobName.Replace("%20", " ");
        }

        private static void Initialize()
        {
            if (driveContainer == null)
            {
                lock(lockobj)
                {
                    if (driveContainer == null)
                    {
                        var endpoint = AppConfiguration.Instance.BDriveStorage;                       
                        driveContainer = new BlobContainerClient(endpoint, AppConfiguration.Instance.DriveContainer);
                        driveThumbContainer = new BlobContainerClient(endpoint, AppConfiguration.Instance.DriveThumbContainer);                        
                        driveContainer.CreateIfNotExists();
                        driveThumbContainer.CreateIfNotExists();
                    }
                }
            }       
        }
    }
}