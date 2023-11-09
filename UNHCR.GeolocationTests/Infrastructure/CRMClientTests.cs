using Azure.Core;
using Azure.Identity;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xrm.Sdk.Query;

namespace UNHCR.GeolocationTests.Infrastructure
{
    [TestClass()]
    public class CRMClientTests
    {
        [TestMethod]
        public void TestCRMTestClient()
        {
            var managedIdentity = new DefaultAzureCredential(new DefaultAzureCredentialOptions()
            {
                ManagedIdentityClientId = "ba497ab9-d3ab-44d7-b52d-1b0fb07c8b80"//Environment.GetEnvironmentVariable("ManagedIdentityClientId")
            });
            var environment = "https://myunhcr-dev.crm4.dynamics.com";//Environment.GetEnvironmentVariable("EnvironmentUrl");

            var client = new ServiceClient(new Uri(environment), tokenProviderFunction: async u =>
               (
                   await managedIdentity.GetTokenAsync(new TokenRequestContext(new[] { $"{environment}/.default" }))).Token
                );
            Assert.IsTrue(client.IsReady);
            var result = client.RetrieveMultiple(new FetchExpression("<fetch mapping='logical'><entity name='progres_buenrollments'></entity></fetch>"));
            Assert.IsNotNull(result);
        }
    }
}
