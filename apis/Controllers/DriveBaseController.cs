namespace apis.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text.Json;
    using Microsoft.AspNetCore.Mvc;

    public class DriveBaseController : ControllerBase
    {
        private HttpResponseMessage CreateResponse<T>(HttpStatusCode code, T content)
        {
            var response = new HttpResponseMessage(code);
            response.Content = new StringContent(JsonSerializer.Serialize(content));
            return response;
        }
    }
}