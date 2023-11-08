using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace UNHCR.Geolocation.Infrastructure
{
    public static class APIHelper
    {
        public static async Task<IActionResult> CallMapsAPI(IHttpClientFactory httpClientFactory, ILogger log, string clientIP, string subKey)
        {
            string azureMapsEndpoint = $"https://atlas.microsoft.com/geolocation/ip/json?api-version=1.0&ip={clientIP}&subscription-key={subKey}";
            string isoCode;
            try
            {
                var httpClient = httpClientFactory.CreateClient();

                HttpResponseMessage response = await httpClient.GetAsync(azureMapsEndpoint);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);

                isoCode = jsonResponse?.countryRegion?.isoCode;

                if (string.IsNullOrEmpty(isoCode))
                {
                    log.LogError("Unable to extract ISO code from Azure Maps");
                    return new BadRequestObjectResult("Error extracting ISO code from Azure Maps");
                }

<<<<<<< HEAD
/*                return new OkObjectResult(new
                {
                    ClientIp = clientIP,
                    IsoCode = isoCode
                });*/

=======
>>>>>>> 6a11881cbb518e48e547fe7b4b2c662920fa7e3c
                var result = new
                {
                    ClientIP = clientIP,
                    IsoCode = isoCode
                };

<<<<<<< HEAD
                return new OkObjectResult(result); 
=======
                log.LogInformation($"before sending to retrieve_isoocde{result}");
                return new OkObjectResult(result);
>>>>>>> 6a11881cbb518e48e547fe7b4b2c662920fa7e3c
            }
            catch (HttpRequestException httpRequestException)
            {
                log.LogError($"Resquest to Azure Maps failed: {httpRequestException.Message}");
                return new BadRequestObjectResult("External service error");
            }
        }
    }
}
