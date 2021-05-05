namespace libs.models
{
    using System;
    using Azure.Data.Tables;

    public class TableEntityBase : IToTableEntity, ITableEntity
    {        
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public Azure.ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }

        public virtual TableEntity ToTableEntity()
        {            
            var tentity =  new TableEntity();
            tentity.ETag = this.ETag;
            tentity.PartitionKey = this.PartitionKey;
            tentity.RowKey = this.RowKey;
            tentity.Timestamp =  this.Timestamp;
            return tentity;
        }
    }
}