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

        //private string clientIP;

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

            var clientIPResult = await IPHelper.GetClientIP(req, log, IPHelper.GetClientIP());
            if (clientIPResult is not OkObjectResult okResult)
            {
                return clientIPResult;
            }

            var clientIP = okResult.Value.ToString();

            log.LogInformation($"The client IP address is {clientIP}");
            var result = await APIHelper.CallMapsAPI(httpClientFactory, log, clientIP, subkey);

            return new OkObjectResult(result);

        }

    }
}
