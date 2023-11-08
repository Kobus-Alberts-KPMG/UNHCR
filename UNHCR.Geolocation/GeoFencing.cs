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
using Azure;
using System.Reflection.Metadata;


namespace UNHCR.Geolocation
{
    public class GeoFencing
    {
        private readonly IKeyVaultManager keyVaultManager;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IDataverseHttpClient dataverseHttpClient;
        private string subkey;

        public GeoFencing(IKeyVaultManager _keyVaultManager, IHttpClientFactory _httpClientFactory, IDataverseHttpClient _dataverseHttpClient)
        {
            keyVaultManager = _keyVaultManager;
            httpClientFactory = _httpClientFactory;
            dataverseHttpClient = _dataverseHttpClient;
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


            var MapsAPIResult = await APIHelper.CallMapsAPI(httpClientFactory, log, clientIP, subkey);
            string isocode = string.Empty;
            if (MapsAPIResult is OkObjectResult okResult2)
            {
                dynamic resultData = okResult2.Value;
                isocode = resultData?.IsoCode;
                isocode = isocode.Trim();
            }

            var dataverseTableResult = await dataverseHttpClient.GetRequestAsync($"api/data/v9.1/progres_buenrollments?$filter=progres_progres_buenrollment_progres_countryterri/any()&$expand=progres_progres_buenrollment_progres_countryterri($filter=progres_isocode2 eq '{isocode}')");
            string dataverseTableContent = await dataverseTableResult.Content.ReadAsStringAsync();
            var enrollmentCheck = JsonConvert.DeserializeObject<EnrollmentCheck>(dataverseTableContent);
            bool isMatchFound = false;

            Console.WriteLine($"body content: {dataverseTableContent}");
            foreach (var enrollmentTerritory in enrollmentCheck.value)
            {
                if (enrollmentTerritory.progres_progres_buenrollment_progres_countryterri.Any())
                {
                    foreach (var isoCode in enrollmentTerritory.progres_progres_buenrollment_progres_countryterri)
                    {
                        isMatchFound = true;
                        log.LogInformation($"Matching Country Found: {isMatchFound} for ISO:{isoCode.progres_isocode3}");
                        break;
                    }
                }
            }
            var result = new
            {
                ClientIP = clientIP,
                IsoCode = isocode,
                MatchFound = isMatchFound
            };
            return  new OkObjectResult(result);
        }
    }
}
