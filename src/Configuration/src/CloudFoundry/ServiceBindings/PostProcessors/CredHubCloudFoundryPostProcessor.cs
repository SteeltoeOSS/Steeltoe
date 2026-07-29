// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

// Unlike other post-processors, CredHub credentials have no fixed schema: the keys are arbitrary secret names
// chosen by whoever created the CredHub credential. In addition to the usual mapping under the namespaced
// "steeltoe:service-bindings:credhub:..." prefix (for traceability), each credential is written to the root of the
// configuration, so it can be read the same way a literal environment variable would be.
internal sealed partial class CredHubCloudFoundryPostProcessor : CloudFoundryPostProcessor
{
    internal const string BindingType = "credhub";

    private readonly ILogger<CredHubCloudFoundryPostProcessor> _logger;

    public CredHubCloudFoundryPostProcessor(ILogger<CredHubCloudFoundryPostProcessor> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public override void PostProcessConfiguration(PostProcessorConfigurationProvider provider, IDictionary<string, string?> configurationData)
    {
        foreach (string bindingKey in FilterKeys(configurationData, BindingType, KeyFilterSources.Tag | KeyFilterSources.Label))
        {
            var mapper = ServiceBindingMapper.Create(configurationData, bindingKey, BindingType);
            string credentialsPrefix = $"{bindingKey}{ConfigurationPath.KeyDelimiter}credentials{ConfigurationPath.KeyDelimiter}";

            foreach ((string key, string? value) in configurationData.ToArray())
            {
                if (!key.StartsWith(credentialsPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string credentialPath = key[credentialsPrefix.Length..];
                mapper.SetToValue(credentialPath, value);

                string normalizedKey = credentialPath.Contains("__", StringComparison.Ordinal)
                    ? credentialPath.Replace("__", ConfigurationPath.KeyDelimiter, StringComparison.Ordinal)
                    : credentialPath;

                if (configurationData.TryGetValue(normalizedKey, out string? existingValue) && existingValue != value)
                {
                    LogOverwritingConfigurationKey(normalizedKey, mapper.BindingName);
                }

                configurationData[normalizedKey] = value;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "CredHub binding '{BindingName}' overwrites configuration key '{Key}', which was already set from a different source.")]
    private partial void LogOverwritingConfigurationKey(string key, string bindingName);
}
