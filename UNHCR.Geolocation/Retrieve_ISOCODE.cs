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
using System.Collections.Generic;
using System.Net.Http;

namespace UNHCR.Geolocation
{
    public static class Retrieve_ISOCODE
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private static readonly string azureMapsSubKey = "Cn25V3RY2C5MitRHKC1eaVJ8YbhmS825BEK_89GkYAw";
        [FunctionName("Retrieve_ISOCODE")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;
            string url = data?.url; // get the URL from the request body



            string ipAddress = req.Headers["X-Forwared-For"].FirstOrDefault();

            var headerValues = new Dictionary<string, string>();

            foreach (var header in req.Headers)
            {
                var headerValue = string.Join(",", header.Value);
                headerValues.Add(header.Key, headerValue);
            }
            string clientIP = string.Empty;

            if (headerValues.TryGetValue("X-Forwarded-For", out string xForwardedForValue))
            {
                clientIP = xForwardedForValue.Split(':')[0];
                log.LogInformation("X-Forwarded-For: " + xForwardedForValue);
            }
            else
            {
                log.LogInformation("The 'X-Forwarded-For' header was not found.");
            }

            string isoCode = string.Empty;
            string azureMapsEndpoint = $"https://atlas.microsoft.com/geolocation/ip/json?api-version=1.0&ip={clientIP}&subscription-key={azureMapsSubKey}";

            try
            {
                HttpResponseMessage response = await HttpClient.GetAsync(azureMapsEndpoint);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

                isoCode = jsonResponse?.countryRegion?.isoCode;

                if (string.IsNullOrEmpty(isoCode))
                {
                    log.LogError("Unable to extract ISO code from Azure Maps");
                    return new BadRequestObjectResult("Error extracting ISO code from Azure Maps");
                }

            }
            catch (HttpRequestException httpRequestException)
            {
                log.LogError($"Resquest to Azure Maps failed: {httpRequestException.Message}");
                return new BadRequestObjectResult("External service error");
            }
            //var json = JsonConvert.SerializeObject(headerValues);
            var result = new
            {
                ClientIp = clientIP,
                IsoCode = isoCode
            };


            var message = $"The client IP address is {clientIP}";
            log.LogInformation(message);

            /*          return new OkObjectResult(json)
            {
                ContentTypes = { "application/json" }
            };*/
            return new OkObjectResult(result);

        }
    }
}
