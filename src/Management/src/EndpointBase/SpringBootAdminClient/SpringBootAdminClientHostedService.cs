// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Http;
using Steeltoe.Management.Endpoint.Health;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient
{
    internal class SpringBootAdminClientHostedService : IHostedService
    {
        private readonly SpringBootAdminClientOptions _options;
        private readonly ManagementEndpointOptions _mgmtOptions;
        private readonly HealthEndpointOptions _healthOptions;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        internal static RegistrationResult RegistrationResult { get; set; }

        public SpringBootAdminClientHostedService(SpringBootAdminClientOptions options, ManagementEndpointOptions mgmtOptions, HealthEndpointOptions healthOptions, HttpClient httpClient, ILogger logger = null)
        {
            _options = options;
            _mgmtOptions = mgmtOptions;
            _healthOptions = healthOptions;
            _httpClient = httpClient;
            _logger = logger ?? NullLogger.Instance;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation("Registering with Spring Boot Admin Server at {0}", _options.Url);
            var basePath = _options.BasePath.TrimEnd('/');
            var app = new Application()
            {
                Name = _options.ApplicationName ?? "Steeltoe",
                HealthUrl = new Uri($"{basePath}{_mgmtOptions.Path}/{_healthOptions.Path}"),
                ManagementUrl = new Uri($"{basePath}{_mgmtOptions.Path}"),
                ServiceUrl = new Uri($"{basePath}/"),
                Metadata = new Dictionary<string, object> { { "startup", DateTime.Now } },
            };
            app.Metadata.Merge(_options.Metadata);

            _httpClient.Timeout = TimeSpan.FromMilliseconds(_options.ConnectionTimeoutMS);
            var result = await _httpClient.PostAsJsonAsync($"{_options.Url}/instances", app);
            if (result.IsSuccessStatusCode)
            {
                RegistrationResult = await result.Content.ReadFromJsonAsync<RegistrationResult>();
            }
            else
            {
                _logger.LogError($"Error registering with SpringBootAdmin {result}");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (RegistrationResult == null || string.IsNullOrEmpty(RegistrationResult.Id))
            {
                return;
            }

            await _httpClient.DeleteAsync($"{_options.Url}/instances/{RegistrationResult.Id}");
        }
    }
}
