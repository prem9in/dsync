namespace jobs
{
    using System;
    using libs.common;
    using libs.models;
    using Azure.Data.Tables;
    using Azure.Storage.Blobs;
    using Azure.Storage.Queues;
    using Microsoft.Extensions.Logging;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using IO=System.IO;


    public class Program
    {
        public static void Main(string[] args)
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(IO.Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            AppConfiguration.Initialize(configuration);

            Console.WriteLine("Hello World!");
            var runtime = RuntimeProvider.Get();

            var deleteProcessor = new Processor<OneDriveItem>(runtime.DeleteQueue, async fileinfo => {
                await OneDrive.DeleteFile(runtime, fileinfo);
                return true;
            }, runtime.Log);

            var syncProcessor = new Processor<FileInfo>(runtime.OutputQueue, async fileinfo => {
                return await Sync.SyncFile(runtime, fileinfo);
            }, runtime.Log);

            var st = Task.Run(async delegate
            {
                await syncProcessor.Start().ConfigureAwait(false);
            });

            var dt = Task.Run(async delegate
            {
                await deleteProcessor.Start().ConfigureAwait(false);
            });

            Task.WaitAll(st, dt);            
            Console.WriteLine("Complete!");
        }        
    }
}
