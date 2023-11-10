using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using UNHCR.Geolocation.Infrastructure;
using Microsoft.PowerPlatform.Dataverse.Client;
using Azure.Identity;
using Azure.Core;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;

namespace UNHCR.Geolocation
{

    public class Rootobject
    {
        public List<Entity> entities { get; set; }
        public bool moreRecords { get; set; }
        public string pagingCookie { get; set; }
        public string minActiveRowVersion { get; set; }
        public int totalRecordCount { get; set; }
        public bool totalRecordCountLimitExceeded { get; set; }
        public string entityName { get; set; }
    }

    public class Entity
    {
        public string logicalName { get; set; }
        public string id { get; set; }
        public List<Attribute> attributes { get; set; }
        public object entityState { get; set; }
        public object[] formattedValues { get; set; }
        public object[] relatedEntities { get; set; }
        public string rowVersion { get; set; }
        public object[] keyAttributes { get; set; }
    }

    public class Attribute
    {
        public string key { get; set; }
        public string value { get; set; }
    }

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

            //// Retrieve the client IP
            var clientIPResult = IPHelper.GetClientIP(req, log);
            if (!(clientIPResult is OkObjectResult okResult))
            {
                return clientIPResult;
            }
            var clientIP = okResult.Value.ToString();
            log.LogInformation($"The client IP address is {clientIP}");

            //// Get the ISO code from the AtlasAPI for the IP
            var MapsAPIResult = await APIHelper.CallMapsAPI(httpClientFactory, log, clientIP, subkey);
            string isocode = string.Empty;
            if (MapsAPIResult is OkObjectResult okResult2)
            {
                dynamic resultData = okResult2.Value;
                isocode = resultData?.IsoCode;
                isocode = isocode.Trim();
            }

            //// Validate the iso code in Dataverse
            try
            {
                var managedIdentityClientID = await keyVaultManager.GetSecretAsync("msawe-primes-selfservice-magidentity");
                log.LogInformation($"{managedIdentityClientID}");
                var managedIdentity = new DefaultAzureCredential(new DefaultAzureCredentialOptions()
                {
                    ManagedIdentityClientId = managedIdentityClientID
                });

                var environment = await keyVaultManager.GetSecretAsync("Scope");
                log.LogInformation($"{environment}");

                var client = new ServiceClient(new Uri(environment), tokenProviderFunction: async u => (
                    await managedIdentity.GetTokenAsync(new TokenRequestContext(new[] { $"{environment}" }))).Token
                );

                var territories = client.RetrieveMultiple(new FetchExpression($"<fetch version=\"1.0\" output-format=\"xml-platform\" mapping=\"logical\" distinct=\"false\">\r\n<entity name=\"progres_countryterritory\">\r\n<attribute name=\"progres_countryterritoryid\" />\r\n<attribute name=\"progres_name\" />\r\n<attribute name=\"progres_isocode3\" />\r\n<attribute name=\"progres_progresguid\" />\r\n<attribute name=\"progres_isocode2\" />\r\n<order attribute=\"progres_name\" descending=\"false\" />\r\n<filter type=\"and\">\r\n<condition attribute=\"progres_isocode2\" operator=\"eq\" value=\"{isocode}\" />\r\n</filter>\r\n</entity>\r\n</fetch>"));

                bool isMatchFound = false;

                if (territories.Entities != null)
                {
                    foreach (var entity in territories.Entities)
                    {             
                        var progres_isocode2 = entity.KeyAttributes["progres_isocode2"].ToString();
                        isMatchFound = true;
                        log.LogInformation($"Matching Country Found: {isMatchFound} for ISO:{progres_isocode2}");
                        break;
                    }
                }
                var result = new
                {
                    ClientIP = clientIP,
                    IsoCode = isocode,
                    MatchFound = isMatchFound
                };
                return new OkObjectResult(result);

            }
            catch (Exception ex)
            {
                log.LogError($"Error during matching Country for ISO:{isocode}");
                log.LogError($"Message: {ex.Message}. Stack:{ex.StackTrace}");
                throw;
            }
        }
    }
}
