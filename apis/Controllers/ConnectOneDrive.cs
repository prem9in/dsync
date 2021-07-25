
namespace apis.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using libs.common;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;

    [ApiController]
    [Route("[controller]")]
    public class ConnectOneDriveController : DriveBaseController
    {
        [HttpGet]
        public HttpResponseMessage Get()
        {                        
            var runtime = RuntimeProvider.Get();
            return OAuth.RedirectToOAuth(runtime);            
        }

        [HttpPost]
        public async Task<HttpResponseMessage> Post()
        {                        
            var runtime = RuntimeProvider.Get();
            var lines = await ReadLinesAsync(this.Request.BodyReader);
            var content = string.Join("\n", lines);
            return await OAuth.CreateConnection(runtime, content);        
        }

        private async Task<List<string>> ReadLinesAsync(PipeReader reader)
        {
            List<string> results = new List<string>();

            while (true)
            {
                ReadResult readResult = await reader.ReadAsync();
                var buffer = readResult.Buffer;

                SequencePosition? position = null;

                do
                {
                    // Look for a EOL in the buffer
                    position = buffer.PositionOf((byte)'\n');

                    if (position != null)
                    {
                        var readOnlySequence = buffer.Slice(0, position.Value);
                        AddStringToList(results, in readOnlySequence);

                        // Skip the line + the \n character (basically position)
                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                }
                while (position != null);


                if (readResult.IsCompleted && buffer.Length > 0)
                {
                    AddStringToList(results, in buffer);
                }

                reader.AdvanceTo(buffer.Start, buffer.End);

                // At this point, buffer will be updated to point one byte after the last
                // \n character.
                if (readResult.IsCompleted)
                {
                    break;
                }
            }

            return results;
        }

        private static void AddStringToList(List<string> results, in ReadOnlySequence<byte> readOnlySequence)
        {
            // Separate method because Span/ReadOnlySpan cannot be used in async methods
            ReadOnlySpan<byte> span = readOnlySequence.IsSingleSegment ? readOnlySequence.First.Span : readOnlySequence.ToArray().AsSpan();
            results.Add(Encoding.UTF8.GetString(span));
        }
    }
}
