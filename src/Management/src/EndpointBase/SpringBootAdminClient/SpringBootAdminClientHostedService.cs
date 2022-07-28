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

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

internal sealed class SpringBootAdminClientHostedService : IHostedService
{
    private readonly SpringBootAdminClientOptions _options;
    private readonly ManagementEndpointOptions _managementOptions;
    private readonly HealthEndpointOptions _healthOptions;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SpringBootAdminClientHostedService> _logger;

    internal static RegistrationResult RegistrationResult { get; set; }

    public SpringBootAdminClientHostedService(SpringBootAdminClientOptions options, ManagementEndpointOptions managementOptions, HealthEndpointOptions healthOptions, HttpClient httpClient = null, ILogger<SpringBootAdminClientHostedService> logger = null)
    {
        _options = options;
        _managementOptions = managementOptions;
        _healthOptions = healthOptions;
        _httpClient = httpClient ?? HttpClientHelper.GetHttpClient(_options.ValidateCertificates, _options.ConnectionTimeoutMs);
        _logger = logger ?? NullLogger<SpringBootAdminClientHostedService>.Instance;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering with Spring Boot Admin Server at {0}", _options.Url);
        var basePath = _options.BasePath.TrimEnd('/');
        var app = new Application
        {
            Name = _options.ApplicationName ?? "Steeltoe",
            HealthUrl = new Uri($"{basePath}{_managementOptions.Path}/{_healthOptions.Path}"),
            ManagementUrl = new Uri($"{basePath}{_managementOptions.Path}"),
            ServiceUrl = new Uri($"{basePath}/"),
            Metadata = new Dictionary<string, object> { { "startup", DateTime.Now } },
        };
        app.Metadata.Merge(_options.Metadata);

        _httpClient.Timeout = TimeSpan.FromMilliseconds(_options.ConnectionTimeoutMs);

        HttpResponseMessage result = null;
        try
        {
            result = await _httpClient.PostAsJsonAsync($"{_options.Url}/instances", app, cancellationToken: cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error connecting to SpringBootAdmin: {Message}", exception.Message);
        }

        if (result is { IsSuccessStatusCode: true })
        {
            RegistrationResult = await result.Content.ReadFromJsonAsync<RegistrationResult>();
        }
        else
        {
            var errorResponse = result != null ? await result.Content.ReadAsStringAsync() : string.Empty;
            _logger.LogError("Error registering with SpringBootAdmin: {Message} \n {Response} ", result?.ToString(), errorResponse);
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
