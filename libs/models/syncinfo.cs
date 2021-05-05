namespace libs.models
{
    using Azure.Data.Tables;
    using System;   
    using System.Collections.Generic;

    public class SyncInfo : TableEntityBase
    {   
        public int Count { get; set; }
        public TimeSpan Duration { get; set; }
        public ulong Size { get; set; }  

        public override TableEntity ToTableEntity()
        {
            var props = new Dictionary<string, object>();
            props.Add("Count", this.Count);
            props.Add("Duration", this.Duration);
            props.Add("Size", this.Size);           
            return new TableEntity(props);
        } 
    }
}