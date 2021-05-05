namespace libs.models
{
    using Azure.Data.Tables;
    
    public interface IToTableEntity
    {
         TableEntity ToTableEntity();
    }
}