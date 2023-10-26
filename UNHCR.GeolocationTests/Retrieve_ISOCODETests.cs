using Microsoft.VisualStudio.TestTools.UnitTesting;
using UNHCR.Geolocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using UNHCR.Geolocation.Infrastructure;

namespace UNHCR.Geolocation.Tests
{
    [TestClass()]
    public class Retrieve_ISOCODETests
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger log;

        public Retrieve_ISOCODETests(IHttpClientFactory httpClientFactory, ILogger log)
        {
            this.httpClientFactory = httpClientFactory;
            this.log = log;
        }

        [TestMethod()]
        public async Task CallMapsAPITest()
        {
            string clientIP = null;
            string subKey = null;
            var result = await APIHelper.CallMapsAPI(httpClientFactory, log, clientIP, subKey);
            Assert.Fail();
        }
    }
}