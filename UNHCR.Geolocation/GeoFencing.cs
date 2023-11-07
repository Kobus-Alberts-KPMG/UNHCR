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
            }


            var dataverseTableResult = await dataverseHttpClient.GetRequestAsync("api/data/v9.1/progres_buenrollments?$select=_owningbusinessunit_value,progres_blockregistration,_progres_businessunit_value,progres_name,_progres_portalcountry_value,progres_registrationlimit&$expand=progres_progres_buenrollment_progres_countryterri($select=progres_isocode2,progres_isocode3,progres_name,progres_progresguid)");
            string dataverseTableContent = await dataverseTableResult.Content.ReadAsStringAsync();
            Console.WriteLine($"body content: {dataverseTableContent}");

            dynamic dataverseJsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject(dataverseTableContent);

            var valuesArray = dataverseJsonObject.value;
            bool isMatchFound = false;
            //string isocode2 = String.Empty;
            

            foreach ( var item in valuesArray ) 
            {
                var countryTerriPropertyInfo = item.GetType().GetProperty("progres_progres_buenrollment_progres_countryterri");
               
                if (countryTerriPropertyInfo != null && countryTerriPropertyInfo.GetValue(item) != null)
                {
                    var isocode2PropertyInfo = countryTerriPropertyInfo.PropertyType.GetProperty("progres_isocode2");

                    var isocode2 = isocode2PropertyInfo?.GetValue(countryTerriPropertyInfo.GetValue(item));

                    if (isocode2 != null && isocode2.ToString().Trim() == isocode)
                    {
                        isMatchFound = true;
                        break;
                    }
                }
            }

        

            log.LogInformation($"Matching Country Found: {isMatchFound}");


            //log.LogInformation($"after recieving to isocode_retrieve {result}");

            //var finalResult = okResult2.Value.ToString();

      
            //return new OkObjectResult(result);
            return  new OkObjectResult(isMatchFound);

        }

    }
}
