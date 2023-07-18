// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Http;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

internal sealed class SpringBootAdminClientHostedService : IHostedService
{
    private readonly SpringBootAdminClientOptions _options;
    private readonly ManagementEndpointOptions _managementOptions;
    private readonly HealthEndpointOptions _healthOptions;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SpringBootAdminClientHostedService> _logger;

    internal static RegistrationResult RegistrationResult { get; set; }

    public SpringBootAdminClientHostedService(SpringBootAdminClientOptions options, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        IOptionsMonitor<HealthEndpointOptions> healthOptions, ILogger<SpringBootAdminClientHostedService> logger, HttpClient httpClient = null)
    {
        ArgumentGuard.NotNull(logger);

        _options = options;
        _managementOptions = managementOptions.CurrentValue;
        _healthOptions = healthOptions.CurrentValue;
        _httpClient = httpClient ?? HttpClientHelper.GetHttpClient(_options.ValidateCertificates, _options.ConnectionTimeoutMs);
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering with Spring Boot Admin Server at {url}", _options.Url);

        string basePath = _options.BasePath.TrimEnd('/');

        var app = new Application
        {
            Name = _options.ApplicationName ?? "Steeltoe",
            HealthUrl = new Uri($"{basePath}{_managementOptions.Path}/{_healthOptions.Path}"),
            ManagementUrl = new Uri($"{basePath}{_managementOptions.Path}"),
            ServiceUrl = new Uri($"{basePath}/"),
            Metadata = new Dictionary<string, object>
            {
                { "startup", DateTime.Now }
            }
        };

        app.Metadata.Merge(_options.Metadata);

        _httpClient.Timeout = TimeSpan.FromMilliseconds(_options.ConnectionTimeoutMs);

        HttpResponseMessage result = null;

        try
        {
            result = await _httpClient.PostAsJsonAsync($"{_options.Url}/instances", app, cancellationToken);
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
            string errorResponse = result != null ? await result.Content.ReadAsStringAsync() : string.Empty;
            _logger.LogError("Error registering with SpringBootAdmin: {Message} \n {Response} ", result?.ToString(), errorResponse);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (RegistrationResult == null || string.IsNullOrEmpty(RegistrationResult.Id))
        {
            return;
        }

        var requestUri = new Uri($"{_options.Url}/instances/{RegistrationResult.Id}");
        await _httpClient.DeleteAsync(requestUri);
    }
}
