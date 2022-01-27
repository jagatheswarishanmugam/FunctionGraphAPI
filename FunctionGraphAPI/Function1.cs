using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;

namespace FunctionGraphAPI
{
    public static class Function1
    {
        [FunctionName("GraphNotificationHook")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // parse query parameter
            var validationToken = req.Query["validationToken"];
            log.LogInformation(validationToken);
            if (!string.IsNullOrEmpty(validationToken))
            {

                log.LogInformation("validationToken: " + validationToken);
                log.LogInformation("Sending validation token");
                return new ContentResult { Content = validationToken, ContentType = "text/plain" };
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<GraphNotification>(requestBody);

            if (!data.value.FirstOrDefault().ClientState.Equals("SecretClientState", StringComparison.OrdinalIgnoreCase))
            {
                log.LogInformation("client state not valid");
                //client state is not valid (doesn't much the one submitted with the subscription)
                return new BadRequestResult();
            }
            //do something with the notification data
            log.LogInformation("sending 200 ok");
            return new OkResult();
        }
    }
}
