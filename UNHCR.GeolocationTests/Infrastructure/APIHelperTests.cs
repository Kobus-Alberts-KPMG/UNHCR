using Microsoft.VisualStudio.TestTools.UnitTesting;
using UNHCR.Geolocation.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Moq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;

namespace UNHCR.Geolocation.Infrastructure.Tests
{
    [TestClass()]
    public class APIHelperTests
    {
        private Mock<IHttpClientFactory> httpClientFactory;
        private ILogger log;

        public APIHelperTests()
        {

            httpClientFactory = new Mock<IHttpClientFactory>();
            var expected = new
            {
                countryRegion = new
                {
                    clientIP = "127.0.0.1",
                    isoCode = "MLT"
                }
            };

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(expected))
                });

            var httpClient = new HttpClient(mockMessageHandler.Object);

            httpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);
            log = new LoggerFactory().CreateLogger("Logger");
        }

        [TestMethod()]
        public async Task CallMapsAPITestAsync()
        {
            string clientIP = "66.131.120.255";
            string subKey = "cg_fL7YZ8O7kcREK3SdY7FRrLFKqAauxjy8kd5sZ8bo";
            var result = await APIHelper.CallMapsAPI(httpClientFactory.Object, log, clientIP, subKey);
            var resultstring = JsonConvert.SerializeObject(result);
            Assert.AreEqual("{\"Value\":{\"ClientIP\":\"66.131.120.255\",\"IsoCode\":\"MLT\"},\"Formatters\":[],\"ContentTypes\":[],\"DeclaredType\":null,\"StatusCode\":200}", resultstring);
        }
    }
}