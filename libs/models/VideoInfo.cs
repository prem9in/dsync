namespace libs.models
{
    using Azure.Data.Tables;
    using System;    
    using libs.common;

    public class VideoInfo : TableEntityBase
    {
        public VideoInfo()
        {            
        }
        
        public VideoInfo(Guid syncId, OneDriveItem driveItem, DateTime timestamp)
        {
            this.PartitionKey = "VideoFiles";
            this.RowKey = Extensions.NormalizeRowKey(driveItem.Id);
            this.Id = driveItem.Id;
            this.Type = driveItem.Type.ToString();
            this.SyncId = syncId;
            if (driveItem.Video != null)
            {
                this.BitRate = driveItem.Video.BitRate;
                this.Width = driveItem.Video.Width;
                this.Height = driveItem.Video.Height;
                this.Duration = driveItem.Video.Duration;
            }
            
            this.Timestamp = timestamp;
            this.ETag = Azure.ETag.All;
        }

        public string Id { get; set; }

        public int BitRate { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Duration { get; set; }

        public string Type { get; set; }

        public Guid SyncId { get; set; }

        public override TableEntity ToTableEntity()
        {
            var tentity = base.ToTableEntity();
            tentity.Add("Id", this.Id);
            tentity.Add("BitRate", this.BitRate);
            tentity.Add("Width", this.Width);
            tentity.Add("Height", this.Height);
            tentity.Add("Duration", this.Duration);            
            tentity.Add("Type", this.Type); 
            tentity.Add("SyncId", this.SyncId);             
            return tentity;
        }
    }

    
}