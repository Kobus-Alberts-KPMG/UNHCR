using Microsoft.AspNetCore.Http;
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
	public static class IPHelper
	{
        public static IActionResult GetClientIP(HttpRequest req, ILogger log)
        {
            try
            {
                var headerValues = new Dictionary<string, string>();

                foreach (var header in req.Headers)
                {
                    var headerValue = string.Join(":", header.Value);
                    headerValues.Add(header.Key, headerValue);
                }

                if (headerValues.TryGetValue("X-Forwarded-For", out string xForwardedForValue))
                {
                    log.LogInformation($"X-Forwarded-For: {xForwardedForValue}");
                    var clientIP = xForwardedForValue.Split(':')[0];
                    log.LogInformation($"Client IP: {clientIP}");
                    return new OkObjectResult(clientIP);
                }
                else
                {
                    log.LogInformation("The 'X-Forwarded-For' header was not found.");
                    return new BadRequestObjectResult("Client IP was not found");

                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error getting client IP");
                return new BadRequestObjectResult("Error getting client IP");
            }
        }
    }
}
