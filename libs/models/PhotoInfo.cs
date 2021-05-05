namespace libs.models
{
    using Azure.Data.Tables;
    using System;    
    using libs.common;

    public class PhotoInfo : TableEntityBase
    {
        public PhotoInfo()
        {            
        } 
        public PhotoInfo(Guid syncId, OneDriveItem driveItem, DateTime timestamp)
        {
            this.PartitionKey = "PhotoFiles";
            this.RowKey = Extensions.NormalizeRowKey(driveItem.Id);
            this.Id = driveItem.Id;
            this.Type = driveItem.Type.ToString();
            this.SyncId = syncId;
            this.Timestamp = timestamp;
            this.ETag = Azure.ETag.All;
            if (driveItem.Image != null)
            {
                this.Width = driveItem.Image.Width;
                this.Height = driveItem.Image.Height;
            }

            if (driveItem.Photo != null)
            {
                this.TakenDateTime = driveItem.Photo.TakenDateTime;
                this.CameraMake = driveItem.Photo.CameraMake;
                this.CameraModel = driveItem.Photo.CameraModel;
                this.Iso = driveItem.Photo.Iso;
                this.FocalLength = driveItem.Photo.FocalLength;
                this.FNumber = driveItem.Photo.FNumber;
            }
        }

        public string Id { get; set; }

        public Guid SyncId { get; set; }

        public int Width { get; set; }
        
        public int Height { get; set; }

        public DateTimeOffset TakenDateTime { get; set; }

        public string CameraMake { get; set; }

        public string CameraModel { get; set; }

        public long Iso { get; set; }

        public double FocalLength { get; set; }

        public double FNumber { get; set; }

        public string Type { get; set; }

        public override TableEntity ToTableEntity()
        {
            var tentity = base.ToTableEntity();
            tentity.Add("Id", this.Id);
            tentity.Add("Width", this.Width);
            tentity.Add("Height", this.Height);
            tentity.Add("TakenDateTime", this.TakenDateTime);
            tentity.Add("CameraMake", this.CameraMake);            
            tentity.Add("CameraModel", this.CameraModel); 
            tentity.Add("Iso", this.Iso); 
            tentity.Add("FocalLength", this.FocalLength); 
            tentity.Add("SyncId", this.SyncId); 
            tentity.Add("Type", this.Type); 
            tentity.Add("FNumber", this.FNumber); 
            return tentity;
        }

    }
}