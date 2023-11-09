﻿using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace UNHCR.Geolocation.Infrastructure
{
    public class DataverseHttpClient : IDataverseHttpClient
    {
        private readonly IHttpClientFactory httpClientFactory;
        private TenantInfo tenantInfo;
        private ILogger<DataverseHttpClient> logger;

        public DataverseHttpClient(IHttpClientFactory httpClientFactory, IKeyVaultManager keyVaultManager, ILogger<DataverseHttpClient> logger)
        {
            this.httpClientFactory = httpClientFactory;
            var tenantId = keyVaultManager.GetSecretAsync("TenantId").GetAwaiter().GetResult();
            var clientId = keyVaultManager.GetSecretAsync("ClientId").GetAwaiter().GetResult();
            var clientSecret = keyVaultManager.GetSecretAsync("ClientSecret").GetAwaiter().GetResult();
            var scope = keyVaultManager.GetSecretAsync("Scope2").GetAwaiter().GetResult();

            TenantInfo _tenantInfo = new()
            {
                TenantId = tenantId,
                ClientId = clientId,
                ClientSecret = clientSecret,
                Scope = scope
            };
            tenantInfo = _tenantInfo;
            this.logger = logger;
        }

        /// <summary>
        /// Retrieves an authentication header from the service.
        /// </summary>
        /// <returns>The authentication header for the Web API call.</returns>
        private async Task<AuthenticationResult> GetAuthenticationHeaderAsync()
        {
            string redirectUri = "https://login.microsoftonline.com/oauth2/v2.0/token";
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(tenantInfo.ClientId)
                .WithClientSecret(tenantInfo.ClientSecret)
                .WithClientId(tenantInfo.ClientId)
                .WithRedirectUri(redirectUri)
                .WithTenantId(tenantInfo.TenantId)
                .Build();
            logger.LogInformation($"retrieved token for client {tenantInfo.ClientId} on tenant {tenantInfo.TenantId}");
            return await app.AcquireTokenForClient(new[] { tenantInfo.Scope }).ExecuteAsync();
        }

        /// <summary>
        /// Get managed identity token.
        /// </summary>
        /// <param name="resource">The AAD resource URI of the resource for which a token should be obtained. </param>
        /// <param name="apiversion">The version of the token API to be used. "2017-09-01" is currently the only version supported.</param>
        /// <param name="clientId">(Optional) The ID of the user-assigned identity to be used. If omitted, the system-assigned identity is used.</param>
        /// <returns>A Bearer token ready to be used with AAD-authenticated REST API calls.</returns>
        public  async Task<string> GetToken(string resource, string apiversion, string clientId = null)
        {
            var httpClient = httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Secret", Environment.GetEnvironmentVariable("MSI_SECRET"));

            string url;
            if (clientId == null)
            {
                // Get system-assigned identity token
                url = String.Format("{0}?resource={1}&api-version={2}", Environment.GetEnvironmentVariable("MSI_ENDPOINT"), resource, apiversion);
            }
            else
            {
                // Get user-assigned identity token
                url = String.Format("{0}?resource={1}&api-version={2}&clientid={3}", Environment.GetEnvironmentVariable("MSI_ENDPOINT"), resource, apiversion, clientId);
            }

            HttpResponseMessage responseMessage = await httpClient.GetAsync(url);
            string content = await responseMessage.Content.ReadAsStringAsync();

            var result = JObject.Parse(content);
            string accessToken = result["access_token"].ToString();

            return accessToken;
        }



        /// <summary>
        /// Post request stream
        /// </summary>
        /// <param name="uri">Enqueue endpoint URI</param>
        /// <param name="authenticationHeader">Authentication header</param>
        /// <param name="bodyStream">Body stream</param>
        /// <param name="message">ActivityMessage context</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PostRequestAsync(string uri, StringContent bodyString)
        {
            try
            {
                if (tenantInfo == null)
                {
                    throw new ArgumentNullException(nameof(tenantInfo));
                }

                if (uri == null)
                {
                    throw new ArgumentNullException(nameof(uri));
                }

                if (bodyString == null)
                {
                    throw new ArgumentNullException(nameof(bodyString));
                }


                var token = await GetAuthenticationHeaderAsync();
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(tenantInfo.Scope?.Replace(".default", ""));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);

                HttpResponseMessage response = await httpClient.PostAsync(uri, bodyString);

                if (response.IsSuccessStatusCode)
                    return response;
                else
                {
                    if (response.Content != null)
                    {
                        var responseMessage = await response.Content.ReadAsStringAsync();
                        return new HttpResponseMessage()
                        {
                            Content = new StringContent(responseMessage, Encoding.ASCII),
                            StatusCode = HttpStatusCode.PreconditionFailed
                        };
                    }
                    else
                    {
                        return new HttpResponseMessage()
                        {
                            Content = new StringContent("{\"Message\":\"Request failed at client.No response content.\"}", Encoding.ASCII),
                            StatusCode = HttpStatusCode.PreconditionFailed
                        };
                    }
                }
            }
            catch (Exception exception)
            {
                logger.LogError($"{DateTime.Now:s} Exception occured: {exception.Message} {exception.StackTrace}", tenantInfo, uri, bodyString);
                throw;
            }
        }

        /// <summary>
        /// Http Get requests for use with JobStatus API
        /// </summary>
        /// <param name="uri">Request URI</param>
        /// <returns>Task of type HttpResponseMessage</returns>
        public async Task<HttpResponseMessage> GetRequestAsync(string uri)
        {
            try
            {
                if (tenantInfo == null)
                {
                    throw new ArgumentNullException(nameof(tenantInfo));
                }

                var token = await GetAuthenticationHeaderAsync();
                var httpClient = httpClientFactory.CreateClient();
                httpClient.BaseAddress = new Uri(tenantInfo.Scope?.Replace(".default", ""));
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);

                HttpResponseMessage response = await httpClient.GetAsync(uri).ConfigureAwait(false);
                
                if (response.IsSuccessStatusCode) 
                {
                    //var response2 = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    //Console.WriteLine($"response 2: {response2}");
                    return response;
                }       
                else
                {
                    if (response.Content != null)
                    {
                        var responseMessage = await response.Content.ReadAsStringAsync();
                        return new HttpResponseMessage()
                        {
                            Content = new StringContent(responseMessage, Encoding.ASCII),
                            StatusCode = HttpStatusCode.PreconditionFailed
                        };
                    }
                    else
                    {
                        return new HttpResponseMessage()
                        {
                            Content = new StringContent("{\"Message\":\"Request failed at client.No response content.\"}", Encoding.ASCII),
                            StatusCode = HttpStatusCode.PreconditionFailed
                        };
                    }
                }
            }
            catch (Exception exception)
            {
                logger.LogError($"{DateTime.Now:s} Exception occured: {exception.Message} {exception.StackTrace}", tenantInfo, uri);
                throw;
            }
        }

        public Task<HttpResponseMessage> GetRequeestAsync(string uri)
        {
            throw new NotImplementedException();
        }
    }
}
