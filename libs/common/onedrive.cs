namespace libs.common
{
    using libs.models;
    using System;
    using System.IO; 
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
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;

    public static class OneDrive
    {

        private static int FolderCount = 0;

        public static async Task<List<OneDriveItem>> GetAllFiles(Runtime runtime)
        {        
            FolderCount = 0;
            var fileList = await GetFilesAndFolders(runtime, new OneDriveItem { Id = string.Empty, Name = "root" }, true);
            return fileList;
        }

        public static async Task<System.IO.Stream> GetThumbnail(Runtime runtime, string id, long size)
        {
            System.IO.Stream thresult = null;
            var thumbnailQuery = GetContentPath(id, true);
            runtime.Log.LogInformation("Getting thumbnails for id: " + id);
            var result = await CallOneDrive<ThumbNails, OneDriveErrorResponse>(runtime, thumbnailQuery);
            if (result != null && result.Item1 != null && result.Item1.Items != null && result.Item1.Items.Count > 0)
            {
                var medium = result.Item1.Items[0].Medium == null ? null : result.Item1.Items[0].Medium.Url;
                if (!string.IsNullOrWhiteSpace(medium))
                {
                    runtime.Log.LogInformation("Downloading thumbnail from " + medium + ", id: " + id);
                    thresult = await Http.MakeRequestForFile(medium, HttpMethod.Get, null, null, null, size);
                }
                else
                {
                    runtime.Log.LogWarning("No medium thumbnail present for id: " + id);
                }
            }
            else
            {
                runtime.Log.LogWarning("No thumbnail present for id: " + id);
            }

            return thresult;
        }

        public static async Task<System.IO.Stream> GetFileContent(Runtime runtime, string id, long size)
        {
            var fileQuery = GetContentPath(id, false);
            runtime.Log.LogInformation("Downloading file from " + fileQuery + ", id: " + id);
            var fileStream = await CallOneDriveForFile(runtime, fileQuery, size);
            return fileStream;
        }

        public static async Task<string> DeleteFile(Runtime runtime, OneDriveItem deleteItem)
        {
            runtime.Log.LogInformation("Deleting " + deleteItem.FullPath);
            string response = null;
            var deleteUrl = string.Format(AppConfiguration.Instance.OneDriveFileDeleteFormat, AppConfiguration.Instance.OneDriveBaseUri, deleteItem.Id);
            var result = await Http.MakeRequest<string, OneDriveErrorResponse>(deleteUrl, HttpMethod.Delete, OAuthHeaders(runtime), null, null);
            if (result.Item2 != null)
            {
                //// Error condition
            }
            else
            {
                response = result.Item1;
            }

            runtime.Log.LogInformation("Deleted successfully " + deleteItem.FullPath);
            return response;
        }

        private static string GetContentPath(string id, bool isThumbnail)
        {
            return isThumbnail ? string.Format(AppConfiguration.Instance.OneDriveFileThumbnailFormat, AppConfiguration.Instance.OneDriveBaseUri, id) :
                string.Format(AppConfiguration.Instance.OneDriveFileContentFormat, AppConfiguration.Instance.OneDriveBaseUri, id);
        }

        private static async Task<List<OneDriveItem>> GetFolderItems(Runtime runtime, string folderId, bool isRoot)
        {
            var folderUrl = GetFolderPath(folderId, isRoot);
            var items = new List<OneDriveItem>();
            var result = await CallOneDrive<DriveItems, OneDriveErrorResponse>(runtime, folderUrl);
            if (result.Item1 == null)
            {
                //// Error condition
            }
            else
            {
                items.AddRange(result.Item1.Items);
                var nextLink = result.Item1.NextLink;
                while(!string.IsNullOrWhiteSpace(nextLink))
                {
                    var nextResult = await CallOneDrive<DriveItems, OneDriveErrorResponse>(runtime, nextLink);
                    if (nextResult.Item1 == null)
                    {
                        //// Error condition
                    }
                    else
                    {
                        items.AddRange(nextResult.Item1.Items);
                        nextLink = nextResult.Item1.NextLink;
                    }
                }
            }

            return items;
        }

        private static async Task<List<OneDriveItem>> GetFilesAndFolders(Runtime runtime, OneDriveItem folder, bool isRoot)
        {
            var files = new List<OneDriveItem>();
            var items = await GetFolderItems(runtime, folder.Id, isRoot);
            var foldersAndFiles = SeparateFoldersAndFiles(items);
            if (foldersAndFiles != null)
            {
                if (foldersAndFiles.Item2 != null && foldersAndFiles.Item2.Any())
                {
                    files.AddRange(foldersAndFiles.Item2);
                    if (!isRoot)
                    {
                        FolderCount++;
                    }
                    runtime.Log.LogInformation(FolderCount + ". Processing Folder: " + folder.Name + ", number of files: " + foldersAndFiles.Item2.Count());
                }

                if (foldersAndFiles.Item1 != null && foldersAndFiles.Item1.Any())
                {
                    var subFiles = await ProcessFolders(foldersAndFiles.Item1, runtime);
                    files.AddRange(subFiles);
                }
            }
            return files;
        }

        private static async Task<List<OneDriveItem>> GetFilesFromFolders(IEnumerable<OneDriveItem> itemList, Runtime runtime)
        {
            var files = new List<OneDriveItem>();
            var fileItemTaskList = new List<Task<List<OneDriveItem>>>();
            itemList.ForEach(f => {
                fileItemTaskList.Add(GetFilesAndFolders(runtime, f, false));
            });

            await Task.WhenAll(fileItemTaskList);
            fileItemTaskList.ForEach(t => {
                files.AddRange(t.Result);
            });
            
            return files;
        }

        private static async Task<List<OneDriveItem>> ProcessFolders(List<OneDriveItem> folders, Runtime runtime)
        {
            var files = new List<OneDriveItem>();
            if (folders != null && folders.Any())
            {
                var taskList = new List<Task<List<OneDriveItem>>>();
                var count = folders.Count;
                for (var i = 0; i < count; i += Constant.OneDriveBatchSize)
                {
                    var foldersBatch = folders.Skip(i).Take(Constant.OneDriveBatchSize > count ? count : Constant.OneDriveBatchSize);
                    taskList.Add(GetFilesFromFolders(foldersBatch, runtime));
                }
                runtime.Log.LogInformation("Folders Count: " + count + ", Total threads: " + taskList.Count + ", BatchSize: " + Constant.OneDriveBatchSize);
                await Task.WhenAll(taskList);
                taskList.ForEach(t => files.AddRange(t.Result));
            }

            return files;
        }

        private static Tuple<List<OneDriveItem>, List<OneDriveItem>> SeparateFoldersAndFiles(List<OneDriveItem> itemList)
        {
            if (itemList != null && itemList.Any())
            {
                var folders = new List<OneDriveItem>();
                var files = new List<OneDriveItem>();
                itemList.ForEach(f => {
                    if (f.IsFolder)
                    {
                        folders.Add(f);
                    }
                    else
                    {
                        files.Add(f);
                    }
                });
                return Tuple.Create(folders, files);
            }

            return null;
        }

        private static async Task<Tuple<TResponse, TError>> CallOneDrive<TResponse, TError>(Runtime runtime, string queryUrl)
        {       
            return await Http.MakeRequest<TResponse, TError>(queryUrl, HttpMethod.Get, OAuthHeaders(runtime), null, null);        
        }

    private static async Task<Stream> CallOneDriveForFile(Runtime runtime, string queryUrl, long size)
    {  
            return await Http.MakeRequestForFile(queryUrl, HttpMethod.Get, OAuthHeaders(runtime), null, null, size);
        }

        private static Dictionary<string, string> OAuthHeaders(Runtime runtime)
        {
            var authHeader = OAuth.GetAccessoken(runtime);
            var headers = new Dictionary<string, string>();
            headers.Add("Authorization", "Bearer " + authHeader);
            return headers;
        }

        private static string GetFolderPath(string id, bool isRoot)
        {
            return isRoot ?
                    string.Format(AppConfiguration.Instance.OneDriveRootFormat, AppConfiguration.Instance.OneDriveBaseUri) :
                    string.Format(AppConfiguration.Instance.OneDriveFolderFormat, AppConfiguration.Instance.OneDriveBaseUri, id);
        }

    
    }
}