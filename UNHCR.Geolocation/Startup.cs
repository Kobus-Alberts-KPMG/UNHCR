﻿using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using UNHCR.Geolocation;

[assembly: FunctionsStartup(typeof(Startup))]

namespace UNHCR.Geolocation
{
    internal class Startup : FunctionsStartup
    {
        internal IConfiguration Configuration { get; private set; }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            Configuration = builder.GetContext().Configuration;

            var config = new ConfigurationBuilder()
                      .SetBasePath(Environment.CurrentDirectory)
                      .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                      .AddEnvironmentVariables()
                      .Build();

            builder.Services.AddSingleton<IConfiguration>(config);

            builder.Services.AddSingleton<IKeyVaultManager, KeyVaultManager>();

            builder.Services.AddHttpClient();
        }
    }
}