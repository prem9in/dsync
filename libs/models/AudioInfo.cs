namespace libs.models
{
    using Azure.Data.Tables;
    using System;
    using libs.common;

    public class AudioInfo : TableEntityBase 
    {
        public AudioInfo()
        {            
        }

        public AudioInfo(Guid syncId, OneDriveItem driveItem, DateTime timestamp)
        {
            this.PartitionKey = "AudioFiles";
            this.RowKey = Extensions.NormalizeRowKey(driveItem.Id);
            this.Id = driveItem.Id;
            this.Type = driveItem.Type.ToString();
            this.SyncId = syncId;
            if (driveItem.Audio != null)
            {
                this.TakenDateTime = driveItem.Audio.TakenDateTime;
                this.Album = driveItem.Audio.Album;
                this.AlbumArtist = driveItem.Audio.AlbumArtist;
                this.Artist = driveItem.Audio.Artist;
                this.Bitrate = driveItem.Audio.Bitrate;
                this.Copyright = driveItem.Audio.Copyright;
                this.Title = driveItem.Audio.Title;
                this.Track = driveItem.Audio.Track;
                this.Year = driveItem.Audio.Year;
                this.Genre = driveItem.Audio.Genre;
                this.HasDrm = driveItem.Audio.HasDrm;
                this.IsVariableBitrate = driveItem.Audio.IsVariableBitrate;
                this.Duration = driveItem.Audio.Duration;
            }
           
            this.Timestamp = timestamp;
            this.ETag = Azure.ETag.All;
        }

        public string Id { get; set; }

        public Guid SyncId { get; set; }

        public string Type { get; set; }

        public DateTimeOffset TakenDateTime { get; set; }
            
        public string Album { get; set; }

        public string AlbumArtist { get; set; }

        public string Artist { get; set; }

        public int Bitrate { get; set; }

        public string Copyright { get; set; }

        public string Title { get; set; }

        public int Track { get; set; }

        public int Year { get; set; }

        public string Genre { get; set; }

        public bool HasDrm { get; set; }

        public bool IsVariableBitrate { get; set; }

        public int Duration { get; set; }

        public override TableEntity ToTableEntity()
        {
            var tentity = base.ToTableEntity();
            tentity.Add("Id", this.Id);
            tentity.Add("Type", this.Type);
            tentity.Add("TakenDateTime", this.TakenDateTime);
            tentity.Add("Album", this.Album);
            tentity.Add("AlbumArtist", this.AlbumArtist);            
            tentity.Add("Artist", this.Artist); 
            tentity.Add("Bitrate", this.Bitrate); 
            tentity.Add("Copyright", this.Copyright); 
            tentity.Add("SyncId", this.SyncId); 
            tentity.Add("Title", this.Title); 
            tentity.Add("Track", this.Track); 
            tentity.Add("Year", this.Year); 
            tentity.Add("Genre", this.Genre); 
            tentity.Add("HasDrm", this.HasDrm); 
            tentity.Add("IsVariableBitrate", this.IsVariableBitrate); 
            tentity.Add("Duration", this.Duration); 
            return tentity;
        }
    }
}