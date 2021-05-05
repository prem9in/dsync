namespace libs.models
{
    using Azure.Data.Tables;
    using System;
    using System.Collections.Generic;
    using libs.common;

    public class FileInfo : TableEntityBase 
    {
        public FileInfo()
        {
        }

        public FileInfo(Guid syncId, OneDriveItem driveItem, DateTime timestamp)
        {
            this.PartitionKey = "DriveFiles";
            this.Id = driveItem.Id;
            this.RowKey = Extensions.NormalizeRowKey(driveItem.Id);
            this.Name = driveItem.FileName;
            this.Extension = driveItem.FileExtension;
            this.FullPath = driveItem.FullPath;
            this.LastModified = driveItem.LastModifiedDateTime;
            this.Size = driveItem.Size;
            this.MimeType = driveItem.MimeType;
            this.LastModifiedBy = driveItem.LastModifiedBy == null ? string.Empty : (driveItem.LastModifiedBy.User == null ? string.Empty : driveItem.LastModifiedBy.User.DisplayName);
            this.SyncId = syncId;
            this.Type = driveItem.Type.ToString();
            this.Timestamp = timestamp;
            this.Blobed = false;
            this.ETag = Azure.ETag.All;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Extension { get; set; }
        public string FullPath { get; set; }    
        public DateTimeOffset LastModified { get; set; }
        public long Size { get; set; }
        public string MimeType { get; set; }      
        public string LastModifiedBy { get; set; }    
        public Guid SyncId { get; set; }
        public string Type { get; set; }
        public bool Blobed { get; set; }

        public override string ToString()
        {
            return string.Format("Parition Key: {0}, RowKey: {1}, Id: {2}, Type: {3}, FullPath: {4}, Size: {5}, MimeType: {6}",
                this.PartitionKey,
                this.RowKey,
                this.Id,
                this.Type,
                this.FullPath,
                this.Size,
                this.MimeType);
        }        

        public override TableEntity ToTableEntity()
        {
            var tentity = base.ToTableEntity();
            tentity.Add("Id", this.Id);
            tentity.Add("Name", this.Name);
            tentity.Add("Extension", this.Extension);
            tentity.Add("FullPath", this.FullPath);
            tentity.Add("LastModified", this.LastModified);            
            tentity.Add("Size", this.Size); 
            tentity.Add("MimeType", this.MimeType); 
            tentity.Add("LastModifiedBy", this.LastModifiedBy); 
            tentity.Add("SyncId", this.SyncId); 
            tentity.Add("Type", this.Type); 
            tentity.Add("Blobed", this.Blobed); 
            return tentity;
        }
    }

    // Custom comparer for the Product class
    public class FileInfoComparer : IEqualityComparer<FileInfo>
    {
        public bool Equals(FileInfo x, FileInfo y)
        {
            return x.RowKey == y.RowKey;
        }

        public int GetHashCode(FileInfo x)
        {
        return x.RowKey.GetHashCode(); 
        }
    }

}