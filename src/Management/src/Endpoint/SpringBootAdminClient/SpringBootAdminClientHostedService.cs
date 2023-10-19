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
    private readonly SpringBootAdminClientOptions _clientOptions;
    private readonly ManagementOptions _managementOptions;
    private readonly HealthEndpointOptions _healthOptions;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SpringBootAdminClientHostedService> _logger;

    internal static RegistrationResult? RegistrationResult { get; set; }

    public SpringBootAdminClientHostedService(IOptions<SpringBootAdminClientOptions> clientOptions, IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<HealthEndpointOptions> healthOptionsMonitor, ILogger<SpringBootAdminClientHostedService> logger, HttpClient? httpClient = null)
    {
        ArgumentGuard.NotNull(clientOptions);
        ArgumentGuard.NotNull(managementOptionsMonitor);
        ArgumentGuard.NotNull(healthOptionsMonitor);
        ArgumentGuard.NotNull(logger);

        _clientOptions = clientOptions.Value;
        _managementOptions = managementOptionsMonitor.CurrentValue;
        _healthOptions = healthOptionsMonitor.CurrentValue;
        _httpClient = httpClient ?? HttpClientHelper.GetHttpClient(clientOptions.Value.ValidateCertificates, clientOptions.Value.ConnectionTimeoutMs);
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registering with Spring Boot Admin Server at {url}", _clientOptions.Url);

        if (_clientOptions.BasePath == null || _clientOptions.ApplicationName == null)
        {
            throw new InvalidOperationException("BasePath and ApplicationName must be provided in options.");
        }

        string basePath = _clientOptions.BasePath.TrimEnd('/');
        Application app = CreateApplication(basePath, _clientOptions.ApplicationName);

        Merge(app.Metadata, _clientOptions.Metadata);

        _httpClient.Timeout = TimeSpan.FromMilliseconds(_clientOptions.ConnectionTimeoutMs);

        HttpResponseMessage? response = null;

        try
        {
            response = await _httpClient.PostAsJsonAsync($"{_clientOptions.Url}/instances", app, cancellationToken);
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogError(exception, "Error connecting to SpringBootAdmin: {Message}", exception.Message);
        }

        if (response is { IsSuccessStatusCode: true })
        {
            RegistrationResult = await response.Content.ReadFromJsonAsync<RegistrationResult>(cancellationToken);
        }
        else
        {
            string errorResponse = response != null ? await response.Content.ReadAsStringAsync(cancellationToken) : string.Empty;
            _logger.LogError("Error registering with SpringBootAdmin: {Message} \n {Response} ", response?.ToString(), errorResponse);
        }
    }

    private Application CreateApplication(string basePath, string applicationName)
    {
        var healthUrl = new Uri($"{basePath}{_managementOptions.Path}/{_healthOptions.Path}");
        var managementUrl = new Uri($"{basePath}{_managementOptions.Path}");
        var serviceUrl = new Uri($"{basePath}/");

        var metadata = new Dictionary<string, object>
        {
            { "startup", DateTime.UtcNow }
        };

        return new Application(applicationName, managementUrl, healthUrl, serviceUrl, metadata);
    }

    private static void Merge<TKey, TValue>(IDictionary<TKey, TValue> to, IDictionary<TKey, TValue> from)
    {
        from.ToList().ForEach(to.Add);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (RegistrationResult == null || string.IsNullOrEmpty(RegistrationResult.Id))
        {
            return;
        }

        var requestUri = new Uri($"{_clientOptions.Url}/instances/{RegistrationResult.Id}");
        await _httpClient.DeleteAsync(requestUri, cancellationToken);
    }
}
