﻿using Azure.Security.KeyVault.Secrets;
using System.Threading.Tasks;

namespace UNHCR.Geolocation
{
    public class KeyVaultManager : IKeyVaultManager
    {
        private readonly SecretClient _secretClient;

        public KeyVaultManager(SecretClient secretClient)
        {
            _secretClient = secretClient;
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            try
            {
                KeyVaultSecret keyValueSecret = await _secretClient.GetSecretAsync(secretName);
                return keyValueSecret.Value;
            }
            catch
            {
                throw;
            }
        }

        public async Task<string> GetDefaultStorageConnection()
        {
            return await GetSecretAsync("AzureBlobStorageConnection");
        }

        public async Task<string> GetDefaultServiceBusConnection()
        {
            return await GetSecretAsync("AzureServiceBusConnection");
        }
    }
}