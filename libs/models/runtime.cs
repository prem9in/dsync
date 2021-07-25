namespace libs.models
{    
    using System.Net.Http;
    using Azure.Data.Tables;
    using Azure.Storage.Queues;
    using Microsoft.Extensions.Logging;    
    
    public class Runtime
    {
        public TableClient AuthConnect { get; set; }
        public TableClient OnedriveConnect { get; set; }
        public TableClient FileInfoTable { get; set; }
        public TableClient syncInfoTable { get; set; }
        public TableClient FileInfoMeta { get; set; }
        public TableClient PhotoInfoMeta { get; set; }
        public QueueClient OutputQueue { get; set; }
        public QueueClient DeleteQueue { get; set; }
        public ILogger Log { get; set; }
    }
}