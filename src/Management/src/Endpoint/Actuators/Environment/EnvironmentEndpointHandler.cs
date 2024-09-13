// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Configuration;
using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Environment;

internal sealed class EnvironmentEndpointHandler : IEnvironmentEndpointHandler
{
    private readonly IOptionsMonitor<EnvironmentEndpointOptions> _optionsMonitor;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<EnvironmentEndpointHandler> _logger;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public EnvironmentEndpointHandler(IOptionsMonitor<EnvironmentEndpointOptions> optionsMonitor, IConfiguration configuration, IHostEnvironment environment,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _configuration = configuration;
        _environment = environment;
        _logger = loggerFactory.CreateLogger<EnvironmentEndpointHandler>();
    }

    public Task<EnvironmentResponse> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        List<string> activeProfiles = [_environment.EnvironmentName];

        _logger.LogTrace("Fetching property sources");
        IList<PropertySourceDescriptor> propertySources = GetPropertySources();
        var response = new EnvironmentResponse(activeProfiles, propertySources);

        return Task.FromResult(response);
    }

    internal IList<PropertySourceDescriptor> GetPropertySources()
    {
        List<PropertySourceDescriptor> results = [];

        foreach (IConfigurationProvider provider in _configuration.EnumerateProviders())
        {
            PropertySourceDescriptor descriptor = GetPropertySourceDescriptor(provider);
            results.Add(descriptor);
        }

        return results;
    }

    public PropertySourceDescriptor GetPropertySourceDescriptor(IConfigurationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var properties = new Dictionary<string, PropertyValueDescriptor>();
        string sourceName = GetPropertySourceName(provider);
        Sanitizer? sanitizer = null;

        foreach (string key in GetFullKeyNames(provider, null, []))
        {
            if (provider.TryGet(key, out string? value))
            {
                if (provider is CompositeConfigurationProvider)
                {
                    // Wraps other providers, but has no key/value storage of its own.
                    continue;
                }

                sanitizer ??= _optionsMonitor.CurrentValue.GetSanitizer();
                string? sanitizedValue = sanitizer.Sanitize(key, value);
                properties.Add(key, new PropertyValueDescriptor(sanitizedValue));
            }
        }

        return new PropertySourceDescriptor(sourceName, properties);
    }

    public string GetPropertySourceName(IConfigurationProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

        return provider is FileConfigurationProvider fileProvider ? $"{provider.GetType().Name}: [{fileProvider.Source.Path}]" : provider.GetType().Name;
    }

    private HashSet<string> GetFullKeyNames(IConfigurationProvider provider, string? rootKey, HashSet<string> initialKeys)
    {
        foreach (string key in provider.GetChildKeys([], rootKey).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string surrogateKey = key;

            if (rootKey != null)
            {
                surrogateKey = $"{rootKey}:{key}";
            }

            GetFullKeyNames(provider, surrogateKey, initialKeys);

            if (!initialKeys.Any(value => value.StartsWith(surrogateKey, StringComparison.Ordinal)))
            {
                initialKeys.Add(surrogateKey);
            }
        }

        return initialKeys;
    }
}
