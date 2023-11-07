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
    public class GeoFencing
    {
        private readonly IKeyVaultManager keyVaultManager;
        private readonly IHttpClientFactory httpClientFactory;
        private string subkey;
        private string clientIP;

        public GeoFencing(IKeyVaultManager _keyVaultManager, IHttpClientFactory _httpClientFactory)
        {
            keyVaultManager = _keyVaultManager;
            httpClientFactory = _httpClientFactory;
        }

        [FunctionName("GeoFencing")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {
            subkey = await keyVaultManager.GetSecretAsync("AtlasSubscriptionKey");

            var clientIPResult = IPHelper.GetClientIP(req, log);
            if (!(clientIPResult is OkObjectResult okResult))
            {
                return clientIPResult;
            }

            var clientIP = okResult.Value.ToString();

            log.LogInformation($"The client IP address is {clientIP}");
            var result = await APIHelper.CallMapsAPI(httpClientFactory, log, clientIP, subkey);
            log.LogInformation($"after recieving to isocode_retrieve {result}");

            //var finalResult = okResult2.Value.ToString();

            log.LogInformation($"{result}");
            //return new OkObjectResult(result);
            return result;

        }

    }
}
