using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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

                var result = new
                {
                    ClientIP = clientIP,
                    IsoCode = isoCode
                };

                log.LogInformation($"before sending to retrieve_isoocde{result}");
                return new OkObjectResult(result);
            }
            catch (HttpRequestException httpRequestException)
            {
                log.LogError($"Resquest to Azure Maps failed: {httpRequestException.Message}");
                return new BadRequestObjectResult("External service error");
            }
        }
    }
}
