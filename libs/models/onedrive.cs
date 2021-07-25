namespace libs.models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;    
    using System.Text.Json.Serialization;    
    using System.IO;
    using libs.common;

    public class ThumbNails
    {
        [JsonPropertyName("value")]
        public List<ThumbNail> Items { get; set; }
    }

    public class ThumbNail
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("small")]
        public ThumbItem Small { get; set; }

        [JsonPropertyName("medium")]
        public ThumbItem Medium { get; set; }

        [JsonPropertyName("large")]
        public ThumbItem Large { get; set; }
    }

    public class ThumbItem
    {
        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }
    }

    public class DriveItems
    {
        [JsonPropertyName("@odata.nextLink")]
        public string NextLink { get; set; }

        [JsonPropertyName("value")]
        public List<OneDriveItem> Items { get; set; }
    }

    //// see defintions https://docs.microsoft.com/en-us/graph/api/resources/driveitem?view=graph-rest-1.0

    public class OneDriveItem
    {
         private static Dictionary<string, FileType> fileTypeMap = new Dictionary<string, FileType>(){
            {"video/mp4", FileType.Video}
        };

        private FileType NormalizeFileType(FileType defaultType)
        {
            var fileTypeKey = this.MimeType.ToLowerInvariant();
            if (fileTypeMap.ContainsKey(fileTypeKey))
            {
                var fileType = fileTypeMap[fileTypeKey];
                return fileType;
            }

            return defaultType;
        }

        [JsonIgnore]
        public string FullPath
        {
            get
            {
                var result = string.Empty;
                if (this.Parent != null && !string.IsNullOrWhiteSpace(this.Parent.Path))
                {
                result = this.Parent.Path.Replace("/drive/root:", string.Empty) + "/" + this.Name;
                }

                return result;
            }
        }

        [JsonIgnore]
        public FileType Type
        {
            get
            {
                var result = FileType.None;
                if (this.IsFile)
                {
                    result = FileType.File;
                }

                if (this.IsVideo)
                {
                    result = FileType.Video;
                }
                else if (this.IsAudio)
                {
                    result = FileType.Audio;
                }
                else if (this.IsImage || this.IsPhoto)
                {
                    result = FileType.Photo;
                } 
                
                return NormalizeFileType(result);
            }
        }

        [JsonIgnore]
        public bool IsFolder
        {
            get
            {
                return this.Folder != null;
            }
        }

        [JsonIgnore]
        public bool IsDeleted
        {
            get
            {
                return this.Deleted != null;
            }
        }

        [JsonIgnore]
        public bool IsFile
        {
            get
            {
                return this.File != null;
            }
        }

        [JsonIgnore]
        public bool IsImage
        {
            get
            {
                return this.Image != null;
            }
        }

        [JsonIgnore]
        public bool IsPhoto
        {
            get
            {
                return this.Photo != null;
            }
        }

        [JsonIgnore]
        public bool IsAudio
        {
            get
            {
                return this.Audio != null;
            }
        }

        [JsonIgnore]
        public bool IsVideo
        {
            get
            {
                return this.Video != null;
            }
        }

        [JsonIgnore]
        public string FolderName
        {
            get
            {
                var result = string.Empty;
                if (this.IsFolder)
                {
                    result = this.Name.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Last();                
                }
                return result;
            }
        }

        [JsonIgnore]
        public string FileExtension
        {
            get
            {
                var result = string.Empty;
                if (this.IsFile)
                {
                    string fileName = this.Name.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Last();
                    var fileNameParts = fileName.Split(new char[] { '.' });
                    result = fileNameParts.Length > 1 ? fileNameParts.Last() : string.Empty;
                }
                return result;
            }
        }

        [JsonIgnore]
        public string FileName
        {
            get
            {
                var result = string.Empty;
                if (this.IsFile)
                {
                    string fileName = this.Name.Split(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).Last();
                    var fileNameParts = fileName.Split(new char[] { '.' });
                    result = fileNameParts.First();
                }
                return result;
            }
        }

        [JsonIgnore]
        public string MimeType
        {
            get
            {
                var result = string.Empty;
                if (this.IsFile)
                {
                    if (string.IsNullOrWhiteSpace(this.File.MimeType))
                    {
                        result = MimeTypeMap.GetMimeTypeFromExtension(this.FileExtension);
                    }
                    else
                    {
                        result = this.File.MimeType;
                    }
                }
                return result;
            }
        }

        [JsonPropertyName("deleted")]
        public DeletedFacet Deleted { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("file")]
        public FileFacet File { get; set; }

        [JsonPropertyName("folder")]
        public FolderFacet Folder { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("lastModifiedBy")]
        public OneDriveUser LastModifiedBy { get; set; }

        [JsonPropertyName("lastModifiedDateTime")]
        public DateTimeOffset LastModifiedDateTime { get; set; }

        [JsonPropertyName("location")]
        public LocationFacet Location { get; set; }

        [JsonPropertyName("image")]
        public ImageFacet Image { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("package")]
        public PackageFacet Package { get; set; }

        [JsonPropertyName("parentReference")]
        public ItemReference Parent { get; set; }

        [JsonPropertyName("photo")]
        public PhotoFacet Photo { get; set; }

        [JsonPropertyName("audio")]
        public AudioFacet Audio { get; set; }

        [JsonPropertyName("root")]
        public RootFacet Root { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("video")]
        public VideoFacet Video { get; set; }

        [JsonPropertyName("@microsoft.graph.downloadUrl")]
        public string ShortLivedDownloadUrl { get; set; }
    }

    public class AudioFacet
    {
        [JsonPropertyName("takenDateTime")]
        public DateTimeOffset TakenDateTime { get; set; }

        [JsonPropertyName("album")]
        public string Album { get; set; }

        [JsonPropertyName("albumArtist")]
        public string AlbumArtist { get; set; }

        [JsonPropertyName("artist")]
        public string Artist { get; set; }

        [JsonPropertyName("bitrate")]
        public int Bitrate { get; set; }

        [JsonPropertyName("copyright")]
        public string Copyright { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("track")]
        public int Track { get; set; }

        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("genre")]
        public string Genre { get; set; }

        [JsonPropertyName("hasDrm")]
        public bool HasDrm { get; set; }

        [JsonPropertyName("isVariableBitrate")]
        public bool IsVariableBitrate { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }
    }


    public class VideoFacet
    {
        [JsonPropertyName("bitrate")]
        public int BitRate { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("duration")]
        public int Duration { get; set; }
    }

    public class RootFacet
    {
    }


    public class PhotoFacet
    {
        [JsonPropertyName("takenDateTime")]
        public DateTimeOffset TakenDateTime { get; set; }

        [JsonPropertyName("cameraMake")]
        public string CameraMake { get; set; }

        [JsonPropertyName("cameraModel")]
        public string CameraModel { get; set; }

        [JsonPropertyName("iso")]
        public long Iso { get; set; }

        [JsonPropertyName("focalLength")]
        public double FocalLength { get; set; }

        [JsonPropertyName("fNumber")]
        public double FNumber { get; set; }
    }

    public class ItemReference
    {
        [JsonPropertyName("driveId")]
        public string DriveId { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("shareId")]
        public string ShareId { get; set; }
    }

    public class ImageFacet
    {
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class LocationFacet
    {
        [JsonPropertyName("altitude")]
        public double Altitude { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    public class FolderFacet
    {
        [JsonPropertyName("childCount")]
        public long ChildCount { get; set; }
    }

    public class PackageFacet
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class FileFacet
    {
        [JsonPropertyName("mimeType")]
        public string MimeType { get; set; }
    }

    public class DeletedFacet
    {
        [JsonPropertyName("state")]
        public string State { get; set; }
    }

    public class OneDriveUser
    {
        [JsonPropertyName("user")]
        public OneDriveIdentity User { get; set; }
    }

    public class OneDriveIdentity
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("id")]
        public string Id { get; set; }
    }
}