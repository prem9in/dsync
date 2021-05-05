namespace libs.models
{
    using Azure.Data.Tables;
    using System;
       
    public class BlobInfo : TableEntityBase 
    {        
        public BlobInfo(FileInfo file)
        {
            this.PartitionKey = "BlobFiles";
            this.RowKey = file.RowKey;
            this.Name = file.Name;
            this.SyncId = file.SyncId;
            this.Type = file.Type;
            this.ETag = Azure.ETag.All;
        }

        public string Name { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public Guid SyncId { get; set; }
        public string Type { get; set; }
        public string Path { get; set; }

        public override TableEntity ToTableEntity()
        {
            var tentity = base.ToTableEntity();
            tentity.Add("Name", this.Name);
            tentity.Add("LastModified", this.LastModified);
            tentity.Add("SyncId", this.SyncId);
            tentity.Add("Type", this.Type);
            tentity.Add("Path", this.Path); 
            return tentity;
        }
    }
}