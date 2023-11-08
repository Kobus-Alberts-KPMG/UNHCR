using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using System.Net;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;

namespace UNHCR.Geolocation.Infrastructure.Tests
{
    [TestClass()]
    public class IPHelperTests
    {
        private MemoryStream _memoryStream;
        private Mock<HttpRequest> _mockRequest;
        private ILogger log;
        private Mock<HttpRequest> mockRequest;

        [TestInitialize]
        public void Init()
        {
            var body = new
            {
                test = "Testbody1"
            };
            var json = JsonConvert.SerializeObject(body);
            var byteArray = Encoding.ASCII.GetBytes(json);

            _memoryStream = new MemoryStream(byteArray);
            _memoryStream.Flush();
            _memoryStream.Position = 0;

            mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(x => x.Body).Returns(_memoryStream);
            mockRequest.Setup(x => x.Headers).Returns(new HeaderDictionary { new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>("X-Forwarded-For", "127.0.0.1") });
            log = new LoggerFactory().CreateLogger("Logger");
        }

        [TestMethod()]
        public async Task GetClientIPTest()
        {
            var result = IPHelper.GetClientIP(mockRequest.Object, log);
            var resultstring = JsonConvert.SerializeObject(result);
            Assert.AreEqual("{\"Value\":\"127.0.0.1\",\"Formatters\":[],\"ContentTypes\":[],\"DeclaredType\":null,\"StatusCode\":200}", resultstring);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _memoryStream.Dispose();
        }
    }
}