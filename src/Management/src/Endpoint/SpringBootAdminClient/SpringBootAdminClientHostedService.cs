// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Extensions;
using Steeltoe.Common.Http;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Configuration;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

internal sealed class SpringBootAdminClientHostedService : IHostedService
{
    internal const string HttpClientName = "SpringBootAdmin";

    private readonly IOptionsMonitor<SpringBootAdminClientOptions> _clientOptionsMonitor;
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly IOptionsMonitor<HealthEndpointOptions> _healthOptionsMonitor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SpringBootAdminClientHostedService> _logger;

    internal RegistrationResult? RegistrationResult { get; private set; }

    public SpringBootAdminClientHostedService(IOptionsMonitor<SpringBootAdminClientOptions> clientOptionsMonitor,
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor, IOptionsMonitor<HealthEndpointOptions> healthOptionsMonitor,
        IHttpClientFactory httpClientFactory, TimeProvider timeProvider, ILogger<SpringBootAdminClientHostedService> logger)
    {
        ArgumentNullException.ThrowIfNull(clientOptionsMonitor);
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(healthOptionsMonitor);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _clientOptionsMonitor = clientOptionsMonitor;
        _managementOptionsMonitor = managementOptionsMonitor;
        _healthOptionsMonitor = healthOptionsMonitor;
        _httpClientFactory = httpClientFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        SpringBootAdminClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;

        if (clientOptions.Url == null)
        {
            throw new InvalidOperationException("Spring Boot Admin Server Url must be provided in options.");
        }

        _logger.LogInformation("Registering with Spring Boot Admin Server at {Url}", clientOptions.Url);

        if (clientOptions.ApplicationName == null || !Uri.TryCreate(clientOptions.BasePath, UriKind.Absolute, out Uri? baseUri))
        {
            throw new InvalidOperationException("BasePath and ApplicationName must be provided in options.");
        }

        Application app = CreateApplication(baseUri, clientOptions.ApplicationName);
        Merge(app.Metadata, clientOptions.Metadata);

        using HttpClient httpClient = CreateHttpClient(clientOptions.ConnectionTimeout);

        HttpResponseMessage? response;

        try
        {
            response = await httpClient.PostAsJsonAsync($"{clientOptions.Url}/instances", app, cancellationToken);
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogError(exception, "Error connecting to Spring Boot Admin Server.");
            return;
        }

        if (response.IsSuccessStatusCode)
        {
            RegistrationResult = await response.Content.ReadFromJsonAsync<RegistrationResult>(cancellationToken);
        }
        else
        {
            string errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Error registering with Spring Boot Admin Server: {Message} \n {Response} ", response.ToString(), errorResponse);
        }
    }

    private Application CreateApplication(Uri baseUri, string applicationName)
    {
        ManagementOptions managementOptions = _managementOptionsMonitor.CurrentValue;
        HealthEndpointOptions healthOptions = _healthOptionsMonitor.CurrentValue;

        var healthUriBuilder = new UriBuilder(baseUri)
        {
            Path = healthOptions.GetEndpointPath(managementOptions.Path)
        };

        var managementUriBuilder = new UriBuilder(baseUri)
        {
            Path = managementOptions.Path
        };

        var metadata = new Dictionary<string, object>
        {
            { "startup", _timeProvider.GetUtcNow().UtcDateTime }
        };

        return new Application(applicationName, managementUriBuilder.Uri, healthUriBuilder.Uri, baseUri, metadata);
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

        SpringBootAdminClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;
        var requestUri = new Uri($"{clientOptions.Url}/instances/{RegistrationResult.Id}");

        using HttpClient httpClient = CreateHttpClient(clientOptions.ConnectionTimeout);

        HttpResponseMessage? response;

        try
        {
            response = await httpClient.DeleteAsync(requestUri, cancellationToken);
        }
        catch (Exception exception) when (!exception.IsCancellation())
        {
            _logger.LogError(exception, "Error connecting to Spring Boot Admin Server: {Message}", exception.Message);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            string errorResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Error deleting from Spring Boot Admin Server: {Message} \n {Response} ", response.ToString(), errorResponse);
        }
    }

    private HttpClient CreateHttpClient(TimeSpan timeout)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient(HttpClientName);
        httpClient.ConfigureForSteeltoe(timeout);
        return httpClient;
    }
}
