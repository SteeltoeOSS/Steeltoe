// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Common.Extensions;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Configuration;

#pragma warning disable S107 // Methods should not have too many parameters

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

internal sealed class SpringBootAdminRefreshRunner
{
    private readonly AppUrlCalculator _appUrlCalculator;
    private readonly SpringBootAdminApiClient _springBootAdminApiClient;
    private readonly IOptionsMonitor<SpringBootAdminClientOptions> _clientOptionsMonitor;
    private readonly IOptionsMonitor<ManagementOptions> _managementOptionsMonitor;
    private readonly IOptionsMonitor<HealthEndpointOptions> _healthOptionsMonitor;
    private readonly TimeProvider _timeProvider;
    private readonly IApplicationInstanceInfo _applicationInstanceInfo;
    private readonly ILogger<SpringBootAdminRefreshRunner> _logger;

    private volatile string? _lastRegistrationId;
    private volatile SpringBootAdminClientOptions? _lastGoodOptions;

    internal string? LastRegistrationId => _lastRegistrationId;
    internal SpringBootAdminClientOptions? LastGoodOptions => _lastGoodOptions;

    public SpringBootAdminRefreshRunner(AppUrlCalculator appUrlCalculator, SpringBootAdminApiClient springBootAdminApiClient,
        IOptionsMonitor<SpringBootAdminClientOptions> clientOptionsMonitor, IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        IOptionsMonitor<HealthEndpointOptions> healthOptionsMonitor, TimeProvider timeProvider, IApplicationInstanceInfo applicationInstanceInfo,
        ILogger<SpringBootAdminRefreshRunner> logger)
    {
        ArgumentNullException.ThrowIfNull(appUrlCalculator);
        ArgumentNullException.ThrowIfNull(springBootAdminApiClient);
        ArgumentNullException.ThrowIfNull(clientOptionsMonitor);
        ArgumentNullException.ThrowIfNull(managementOptionsMonitor);
        ArgumentNullException.ThrowIfNull(healthOptionsMonitor);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(applicationInstanceInfo);
        ArgumentNullException.ThrowIfNull(logger);

        _appUrlCalculator = appUrlCalculator;
        _springBootAdminApiClient = springBootAdminApiClient;
        _clientOptionsMonitor = clientOptionsMonitor;
        _managementOptionsMonitor = managementOptionsMonitor;
        _healthOptionsMonitor = healthOptionsMonitor;
        _timeProvider = timeProvider;
        _applicationInstanceInfo = applicationInstanceInfo;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Validating options.");
        SpringBootAdminClientOptions clientOptions = _clientOptionsMonitor.CurrentValue;
        ValidateAndSetOptions(clientOptions);

        if (_lastGoodOptions?.Url != null && !string.Equals(_lastGoodOptions.Url, clientOptions.Url, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Spring Boot Admin Server URL changed from {LastUrl} to {NewUrl}, unregistering first.", _lastGoodOptions.Url, clientOptions.Url);
            await SafeUnregisterAsync(_lastGoodOptions, cancellationToken);
        }

        await RegisterAsync(clientOptions, cancellationToken);
    }

    private void ValidateAndSetOptions(SpringBootAdminClientOptions options)
    {
        // Not using IConfigureOptions here, because the calculation of BaseUrl requires that ASP.NET dynamic port bindings have been assigned,
        // which happens just before the app has fully started. Instead of lazily binding each time configuration has changed
        // (which may happen too soon, and then never again), we rebind on each registration to ensure configuration changes are taken into account.

        List<string> errors = [];

        if (string.IsNullOrEmpty(options.Url))
        {
            errors.Add($"{nameof(SpringBootAdminClientOptions.Url)} must be configured to register with Spring Boot Admin server");
        }
        else if (!Uri.TryCreate(options.Url, UriKind.Absolute, out _))
        {
            errors.Add($"{nameof(SpringBootAdminClientOptions.Url)} must be configured as a fully-qualified URL to register with Spring Boot Admin server");
        }

        if (options.BaseScheme is not null and not "http" and not "https")
        {
            errors.Add($"{nameof(SpringBootAdminClientOptions.BaseScheme)} must be null, 'http' or 'https'");
        }

        if (options.BasePort is not null and not (> 0 and < 65536))
        {
            errors.Add($"{nameof(SpringBootAdminClientOptions.BasePort)} must be in range 1-65535");
        }

        options.ApplicationName ??= _applicationInstanceInfo.ApplicationName;
        options.BaseUrl ??= _appUrlCalculator.AutoDetectAppUrl(options);

        if (string.IsNullOrEmpty(options.BaseUrl))
        {
            errors.Add($"{nameof(SpringBootAdminClientOptions.BaseUrl)} must be configured to register with Spring Boot Admin server");
        }
        else if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
        {
            errors.Add($"{nameof(SpringBootAdminClientOptions.BaseUrl)} must be configured as a fully-qualified URL to register with Spring Boot Admin server");
        }

        if (errors.Count > 0)
        {
            throw new OptionsValidationException(Options.DefaultName, typeof(SpringBootAdminClientOptions), errors);
        }
    }

    private async Task RegisterAsync(SpringBootAdminClientOptions clientOptions, CancellationToken cancellationToken)
    {
        Application app = CreateApplication(new Uri(clientOptions.BaseUrl!), clientOptions);

        _logger.LogInformation("Registering with Spring Boot Admin Server at {Url}.", clientOptions.Url);
        _lastRegistrationId = await _springBootAdminApiClient.RegisterAsync(app, clientOptions, cancellationToken);
        _lastGoodOptions = clientOptions;
    }

    private Application CreateApplication(Uri baseUri, SpringBootAdminClientOptions clientOptions)
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
            ["startup"] = _timeProvider.GetUtcNow().UtcDateTime
        };

        foreach ((string key, object value) in clientOptions.Metadata)
        {
            metadata[key] = value;
        }

        return new Application(clientOptions.ApplicationName!, managementUriBuilder.Uri, healthUriBuilder.Uri, baseUri, metadata);
    }

    public async Task CleanupAsync(CancellationToken cancellationToken)
    {
        if (_lastGoodOptions != null)
        {
            await SafeUnregisterAsync(_lastGoodOptions, cancellationToken);
        }
    }

    private async Task SafeUnregisterAsync(SpringBootAdminClientOptions clientOptions, CancellationToken cancellationToken)
    {
        if (_lastRegistrationId != null)
        {
            try
            {
                _logger.LogInformation("Unregistering from Spring Boot Admin Server at {Url}.", clientOptions.Url);
                await _springBootAdminApiClient.UnregisterAsync(_lastRegistrationId, clientOptions, cancellationToken);
            }
            catch (Exception exception)
            {
                if (!exception.IsCancellation())
                {
                    _logger.LogWarning(exception, "Failed to unregister from Spring Boot Admin server at {Url}.", clientOptions.Url);
                }
            }
            finally
            {
                _lastRegistrationId = null;
            }
        }
    }
}
