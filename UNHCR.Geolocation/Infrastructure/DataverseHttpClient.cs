using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
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
            var scope = keyVaultManager.GetSecretAsync("Scope").GetAwaiter().GetResult();
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
