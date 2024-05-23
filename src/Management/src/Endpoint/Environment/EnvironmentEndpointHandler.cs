// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Configuration;

namespace Steeltoe.Management.Endpoint.Environment;

internal sealed class EnvironmentEndpointHandler : IEnvironmentEndpointHandler
{
    private readonly IOptionsMonitor<EnvironmentEndpointOptions> _optionsMonitor;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly Sanitizer _sanitizer;
    private readonly ILogger<EnvironmentEndpointHandler> _logger;

    public EndpointOptions Options => _optionsMonitor.CurrentValue;

    public EnvironmentEndpointHandler(IOptionsMonitor<EnvironmentEndpointOptions> optionsMonitor, IConfiguration configuration, IHostEnvironment environment,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(optionsMonitor);
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(environment);
        ArgumentGuard.NotNull(loggerFactory);

        _optionsMonitor = optionsMonitor;
        _configuration = configuration;
        _environment = environment;
        _sanitizer = new Sanitizer(optionsMonitor.CurrentValue.KeysToSanitize);
        _logger = loggerFactory.CreateLogger<EnvironmentEndpointHandler>();
    }

    public Task<EnvironmentResponse> InvokeAsync(object? argument, CancellationToken cancellationToken)
    {
        IList<string> activeProfiles = new List<string>
        {
            _environment.EnvironmentName
        };

        _logger.LogTrace("Fetching property sources");
        IList<PropertySourceDescriptor> propertySources = GetPropertySources();
        var response = new EnvironmentResponse(activeProfiles, propertySources);

        return Task.FromResult(response);
    }

    internal IList<PropertySourceDescriptor> GetPropertySources()
    {
        var results = new List<PropertySourceDescriptor>();

        if (_configuration is IConfigurationRoot root)
        {
            List<IConfigurationProvider> providers = root.Providers.ToList();

            if (providers.Exists(provider => provider is IPlaceholderResolverProvider))
            {
                IConfigurationProvider placeholderProvider = providers.First(provider => provider is IPlaceholderResolverProvider);
                providers.InsertRange(0, ((IPlaceholderResolverProvider)placeholderProvider).Providers);
            }

            foreach (IConfigurationProvider provider in providers)
            {
                PropertySourceDescriptor descriptor = GetPropertySourceDescriptor(provider);
                results.Add(descriptor);
            }
        }

        return results;
    }

    public PropertySourceDescriptor GetPropertySourceDescriptor(IConfigurationProvider provider)
    {
        ArgumentGuard.NotNull(provider);

        var properties = new Dictionary<string, PropertyValueDescriptor>();
        string sourceName = GetPropertySourceName(provider);

        foreach (string key in GetFullKeyNames(provider, null, new HashSet<string>()))
        {
            if (provider.TryGet(key, out string? value))
            {
                if (provider is IPlaceholderResolverProvider placeholderProvider && !placeholderProvider.ResolvedKeys.Contains(key))
                {
                    continue;
                }

                string? sanitizedValue = _sanitizer.Sanitize(key, value);
                properties.Add(key, new PropertyValueDescriptor(sanitizedValue));
            }
        }

        return new PropertySourceDescriptor(sourceName, properties);
    }

    public string GetPropertySourceName(IConfigurationProvider provider)
    {
        ArgumentGuard.NotNull(provider);

        return provider is FileConfigurationProvider fileProvider ? $"{provider.GetType().Name}: [{fileProvider.Source.Path}]" : provider.GetType().Name;
    }

    private HashSet<string> GetFullKeyNames(IConfigurationProvider provider, string? rootKey, HashSet<string> initialKeys)
    {
        foreach (string key in provider.GetChildKeys(Enumerable.Empty<string>(), rootKey).Distinct(StringComparer.OrdinalIgnoreCase))
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
