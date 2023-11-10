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
using Microsoft.Extensions.Configuration;
using System.Net.Http;

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

            // Ensure that you configure the correct Salesforce REST endpoint
            string salesforceRestEndpoint = "https://vcollab-1a-dev-ed.develop.my.salesforce.com/services/apexrest/api/GraphNotificationHook";

            var config = new ConfigurationBuilder().AddEnvironmentVariables()
                .Build();

            // parse query parameter
            var validationToken = req.Query["validationToken"];

            var secret = config["MySecret"]; // Stored the secret in the keyvault
            log.LogInformation("Expected Secret: " + secret);

            log.LogInformation(validationToken);
            if (!string.IsNullOrEmpty(validationToken))
            {
                log.LogInformation("validationToken: " + validationToken);
                log.LogInformation("Sending validation token");

                return new ContentResult { Content = validationToken, ContentType = "text/plain" };
            }

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<GraphNotification>(requestBody);
            foreach (var notification in data.value)
            {
                log.LogInformation("Received Client State: " + notification.ClientState);

                if (notification.Resource.Any())
                {
                    log.LogInformation($"Received Notification : '{notification.Resource}', {notification.Id}");
                    log.LogInformation("Change Type" + notification.ChangeType.Value.ToString());
                }

                if (notification.LifecycleEvent.HasValue)
                {
                    log.LogInformation($"Missed Notification Alert: '{notification.LifecycleEvent}', {notification.SubscriptionExpirationDateTime}");
                    log.LogInformation("Missed Type" + notification.LifecycleEvent.Value.ToString());
                }
            }

            if (!data.value.FirstOrDefault().ClientState.Equals(secret, StringComparison.OrdinalIgnoreCase))
            {
                log.LogInformation("Client state not valid");
                // Client state is not valid (doesn't match the one submitted with the subscription)
                return new BadRequestResult();
            }

            // Create an HttpClient to send a POST request to Salesforce REST endpoint
            using (var httpClient = new HttpClient())
            {
                var salesforceAccessToken = "00D5i00000E0TwG!AR4AQC9yEzCq6ld._OI3Lx45Hm4SHDjvjZxhMQoQBvbwAa7imoZe57NHsoJ._Lte6RiKS.h95tS1VOOP4eONBBcvYYq_BBrT"; // Replace with your actual Salesforce access token
                log.LogInformation("Salesforce Access Token: " + salesforceAccessToken);

                // Include Salesforce access token in the "Authorization" header
                httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + salesforceAccessToken);

                var response = await httpClient.PostAsync(salesforceRestEndpoint, new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json"));

                log.LogInformation("Request to Salesforce:");
                log.LogInformation("Request URL: " + salesforceRestEndpoint);
                log.LogInformation("Request Body: " + requestBody);

                if (response.IsSuccessStatusCode)
                {
                    log.LogInformation("Successfully sent data to Salesforce.");
                    return new OkResult();
                }
                else
                {
                    log.LogError("Error sending data to Salesforce. Status code: " + response.StatusCode);
                    log.LogError("Response Content: " + await response.Content.ReadAsStringAsync());
                    return new StatusCodeResult((int)response.StatusCode);
                }
            }
        }
    }
}
