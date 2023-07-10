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
    private readonly IOptionsMonitor<EnvironmentEndpointOptions> _options;
    private readonly IConfiguration _configuration;
    private readonly Sanitizer _sanitizer;

    private readonly IHostEnvironment _environment;
    private readonly ILogger<EnvironmentEndpointHandler> _logger;

    public HttpMiddlewareOptions Options => _options.CurrentValue;

    public EnvironmentEndpointHandler(IOptionsMonitor<EnvironmentEndpointOptions> options, IConfiguration configuration, IHostEnvironment environment,
        ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(environment);
        ArgumentGuard.NotNull(loggerFactory);
        ArgumentGuard.NotNull(options);

        _options = options;
        _configuration = configuration;
        _environment = environment;
        _sanitizer = new Sanitizer(options.CurrentValue.KeysToSanitize);
        _logger = loggerFactory.CreateLogger<EnvironmentEndpointHandler>();
    }

    private EnvironmentResponse DoInvoke(IConfiguration configuration)
    {
        IList<string> activeProfiles = new List<string>
        {
            _environment.EnvironmentName
        };

        _logger.LogTrace("Fetching property sources");
        IList<PropertySourceDescriptor> propertySources = GetPropertySources(configuration);
        return new EnvironmentResponse(activeProfiles, propertySources);
    }

    internal IList<PropertySourceDescriptor> GetPropertySources(IConfiguration configuration)
    {
        var results = new List<PropertySourceDescriptor>();

        if (configuration is IConfigurationRoot root)
        {
            List<IConfigurationProvider> providers = root.Providers.ToList();

            if (providers.Any(p => p is IPlaceholderResolverProvider))
            {
                IConfigurationProvider placeholderProvider = providers.First(p => p is IPlaceholderResolverProvider);
                providers.InsertRange(0, ((IPlaceholderResolverProvider)placeholderProvider).Providers);
            }

            foreach (IConfigurationProvider provider in providers)
            {
                PropertySourceDescriptor psd = GetPropertySourceDescriptor(provider);

                if (psd != null)
                {
                    results.Add(psd);
                }
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
            if (provider.TryGet(key, out string value))
            {
                if (provider is IPlaceholderResolverProvider placeHolderProvider && !placeHolderProvider.ResolvedKeys.Contains(key))
                {
                    continue;
                }

                KeyValuePair<string, string> sanitized = _sanitizer.Sanitize(new KeyValuePair<string, string>(key, value));
                properties.Add(sanitized.Key, new PropertyValueDescriptor(sanitized.Value));
            }
        }

        return new PropertySourceDescriptor(sourceName, properties);
    }

    public string GetPropertySourceName(IConfigurationProvider provider)
    {
        ArgumentGuard.NotNull(provider);
        return provider is FileConfigurationProvider fileProvider ? $"{provider.GetType().Name}: [{fileProvider.Source.Path}]" : provider.GetType().Name;
    }

    private HashSet<string> GetFullKeyNames(IConfigurationProvider provider, string rootKey, HashSet<string> initialKeys)
    {
        foreach (string key in provider.GetChildKeys(Enumerable.Empty<string>(), rootKey).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string surrogateKey = key;

            if (rootKey != null)
            {
                surrogateKey = $"{rootKey}:{key}";
            }

            GetFullKeyNames(provider, surrogateKey, initialKeys);

            if (!initialKeys.Any(k => k.StartsWith(surrogateKey, StringComparison.Ordinal)))
            {
                initialKeys.Add(surrogateKey);
            }
        }

        return initialKeys;
    }

    public async Task<EnvironmentResponse> InvokeAsync(object argument, CancellationToken cancellationToken)
    {
        return await Task.FromResult(DoInvoke(_configuration));
    }
}
