using System.Threading.Tasks;

namespace UNHCR.Geolocation.Infrastructure
{
    public interface IKeyVaultManager
    {
        public Task<string> GetSecretAsync(string secretName);
        public Task<string> GetDefaultStorageConnection();
        public Task<string> GetDefaultServiceBusConnection();
    }
}