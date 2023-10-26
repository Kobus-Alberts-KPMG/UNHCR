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
using UNHCR.Geolocation.Infrastructure;

namespace UNHCR.Geolocation
{
    public class Retrieve_ISOCODE
    {
        private readonly IKeyVaultManager keyVaultManager;
        private readonly IHttpClientFactory httpClientFactory;
        private string subkey;

        private string clientIP;

        public Retrieve_ISOCODE(IKeyVaultManager _keyVaultManager, IHttpClientFactory _httpClientFactory)
        {
            keyVaultManager = _keyVaultManager;
            httpClientFactory = _httpClientFactory;
        }

        [FunctionName("Retrieve_ISOCODE")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {

            subkey = await keyVaultManager.GetSecretAsync("AtlasSubscriptionKey");

            var headerValues = new Dictionary<string, string>();

            foreach (var header in req.Headers)
            {
                var headerValue = string.Join(",", header.Value);
                headerValues.Add(header.Key, headerValue);
            }
            
            if (headerValues.TryGetValue("X-Forwarded-For", out string xForwardedForValue))
            {
                clientIP = xForwardedForValue.Split(':')[0];
                log.LogInformation($"X-Forwarded-For: {xForwardedForValue}");
            }
            else
            {
                log.LogInformation("The 'X-Forwarded-For' header was not found.");
            }

            var result = await APIHelper.CallMapsAPI(httpClientFactory, log, clientIP, subkey);

            var message = $"The client IP address is {clientIP}";
            log.LogInformation(message);

            return new OkObjectResult(result);

        }

    }
}
