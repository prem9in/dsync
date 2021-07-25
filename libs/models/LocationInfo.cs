namespace libs.models
{
    using System;
    using Azure.Data.Tables;
    using libs.common;

    public class LocationInfo : TableEntityBase
    {       
        public LocationInfo()
        {            
        } 
        public LocationInfo(Guid syncId, OneDriveItem driveItem, DateTime timestamp)
        {
            this.PartitionKey = "FileLocation";
            this.RowKey = Extensions.NormalizeRowKey(driveItem.Id);
            this.Id = driveItem.Id;
            this.SyncId = syncId;
            if (driveItem.Location != null)
            {
                this.Altitude = driveItem.Location.Altitude;
                this.Latitude = driveItem.Location.Latitude;
                this.Longitude = driveItem.Location.Longitude;
            }
           
            this.Timestamp = timestamp;
            this.ETag = Azure.ETag.All;
        }

        public string Id { get; set; }

        public double Altitude { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public Guid SyncId { get; set; }

        public override TableEntity ToTableEntity()
        {
            var tentity = base.ToTableEntity();
            tentity.Add("Id", this.Id);           
            tentity.Add("Altitude", this.Altitude); 
            tentity.Add("Latitude", this.Latitude); 
            tentity.Add("Longitude", this.Longitude); 
            tentity.Add("SyncId", this.SyncId); 
            return tentity;
        }
    }
}