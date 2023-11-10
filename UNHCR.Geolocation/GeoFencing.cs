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
using Microsoft.PowerPlatform.Dataverse.Client;
using Azure.Identity;
using Microsoft.PowerPlatform.Dataverse.Client.Extensions;
using Azure.Core;
using Microsoft.Xrm.Sdk.Query;

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

        [FunctionName("PingCRM")]
        public async Task<IActionResult> PingCRM(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {
            var managedIdentity = new DefaultAzureCredential(new DefaultAzureCredentialOptions()
            {
                ManagedIdentityClientId = "ba497ab9-d3ab-44d7-b52d-1b0fb07c8b80"
            });

            var environment = "https://myunhcr-dev.crm4.dynamics.com";

            var client = new ServiceClient(new Uri(environment), tokenProviderFunction: async u => (
                await managedIdentity.GetTokenAsync(new TokenRequestContext(new[] { $"{environment}/.default" }))).Token
            );
            var result = client.RetrieveMultiple(new FetchExpression("<fetch version=\"1.0\" output-format=\"xml-platform\" mapping=\"logical\" distinct=\"false\">\r\n<entity name=\"progres_countryterritory\">\r\n<attribute name=\"progres_countryterritoryid\" />\r\n<attribute name=\"progres_name\" />\r\n<attribute name=\"progres_isocode3\" />\r\n<attribute name=\"progres_progresguid\" />\r\n<attribute name=\"progres_isocode2\" />\r\n<order attribute=\"progres_name\" descending=\"false\" />\r\n<filter type=\"and\">\r\n<condition attribute=\"progres_isocode2\" operator=\"eq\" value=\"MT\" />\r\n</filter>\r\n</entity>\r\n</fetch>"));

            return new OkObjectResult(result);
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
