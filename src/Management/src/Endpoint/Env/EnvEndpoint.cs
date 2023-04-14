// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Configuration;

namespace Steeltoe.Management.Endpoint.Env;

public class EnvEndpoint : IEnvEndpoint
{
    private readonly IOptionsMonitor<EnvEndpointOptions> _options;
    private readonly IConfiguration _configuration;
    private readonly Sanitizer _sanitizer;

    private readonly IHostEnvironment _env;
    private readonly ILogger<EnvEndpoint> _logger;

    public IEndpointOptions Options => _options.CurrentValue;

    public EnvEndpoint(IOptionsMonitor<EnvEndpointOptions> options, IConfiguration configuration, IHostEnvironment env, ILogger<EnvEndpoint> logger)
    {
        ArgumentGuard.NotNull(configuration);
        ArgumentGuard.NotNull(env);
        ArgumentGuard.NotNull(logger);
        ArgumentGuard.NotNull(options);
        _options = options;
        _configuration = configuration;
        _env = env;
        _sanitizer = new Sanitizer(options.CurrentValue.KeysToSanitize);
        _logger = logger;
    }

    public virtual EnvironmentDescriptor Invoke()
    {
        return DoInvoke(_configuration);
    }

    private EnvironmentDescriptor DoInvoke(IConfiguration configuration)
    {
        IList<string> activeProfiles = new List<string>
        {
            _env.EnvironmentName
        };

        _logger.LogTrace("Fetching property sources");
        IList<PropertySourceDescriptor> propertySources = GetPropertySources(configuration);
        return new EnvironmentDescriptor(activeProfiles, propertySources);
    }

    public virtual IList<PropertySourceDescriptor> GetPropertySources(IConfiguration configuration)
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

    public virtual PropertySourceDescriptor GetPropertySourceDescriptor(IConfigurationProvider provider)
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

    public virtual string GetPropertySourceName(IConfigurationProvider provider)
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
}
