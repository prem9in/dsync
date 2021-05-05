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
    using Microsoft.Extensions.Logging.Abstractions;
    using System.Net.Http;

    public static class RuntimeProvider
    {
        private static object lockobj = new object();
        private static Runtime _runtime = null;

        public static Runtime Get()
        {
            if (_runtime == null)
            {
                lock(lockobj)
                {
                    if (_runtime == null)
                    {
                        var cdiskconstring = "";
                        var cdiskTokenTable = new TableClient(cdiskconstring, "oneDriveToken");
                        var cdiskDriveTable = new TableClient(cdiskconstring, "DriveFiles");
                        var cdiskFileQueue = new QueueClient(cdiskconstring, "filesqueue");
                        var cdiskDeleteQueue = new QueueClient(cdiskconstring, "filesdeletequeue");
                        _runtime = new Runtime()
                        {
                            AuthConnect = cdiskTokenTable,
                            OnedriveConnect = cdiskTokenTable,
                            FileInfoTable = cdiskDriveTable,
                            syncInfoTable = cdiskDriveTable,
                            FileInfoMeta = cdiskDriveTable,
                            PhotoInfoMeta = cdiskDriveTable,
                            OutputQueue = cdiskFileQueue,
                            DeleteQueue = cdiskDeleteQueue,
                            Log = new Logger<Runtime>()
                        };                        
                    }
                }
            }

            _runtime.Request = null;
            return _runtime;
        }

        public static Runtime Get(HttpRequestMessage request)
        {
            var result = RuntimeProvider.Get();
            result.Request = request;
            return result;
        }
    }
}