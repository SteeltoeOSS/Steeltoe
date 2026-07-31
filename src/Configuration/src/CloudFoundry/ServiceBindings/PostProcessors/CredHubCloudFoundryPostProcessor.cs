// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Configuration.CloudFoundry.ServiceBindings.PostProcessors;

// CredHub credentials have no fixed schema: the keys are arbitrary secret names chosen by whoever created the CredHub credential.
// Each credential is written to the root of the configuration, so it can be read the same way a literal environment variable would be.
// Dots in a credential key are converted to colons so secrets can be shared between Spring and .NET apps. To keep a literal dot, wrap
// that part of the key in square brackets, mirroring Spring's escape convention for map keys.
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
            string bindingName = configurationData[ConfigurationPath.Combine(bindingKey, "name")] ?? string.Empty;
            string credentialsPrefix = $"{bindingKey}{ConfigurationPath.KeyDelimiter}credentials{ConfigurationPath.KeyDelimiter}";

            foreach ((string key, string? value) in configurationData.ToArray())
            {
                if (!key.StartsWith(credentialsPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string credentialPath = key[credentialsPrefix.Length..];
                string normalizedKey = NormalizeCredentialPath(credentialPath);

                if (configurationData.TryGetValue(normalizedKey, out string? existingValue) && existingValue != value)
                {
                    LogOverwritingConfigurationKey(normalizedKey, bindingName);
                }

                configurationData[normalizedKey] = value;
            }
        }
    }

    // Converts dots to colons, except inside square brackets, which escape a literal dot (matching Spring's map-key escape convention).
    private static string NormalizeCredentialPath(string credentialPath)
    {
        if (!credentialPath.Contains('.', StringComparison.Ordinal) && !credentialPath.Contains('[', StringComparison.Ordinal))
        {
            return credentialPath;
        }

        var builder = new StringBuilder(credentialPath.Length);
        int index = 0;

        while (index < credentialPath.Length)
        {
            char character = credentialPath[index];

            if (character == '[')
            {
                int closingIndex = credentialPath.IndexOf(']', index + 1);

                if (closingIndex != -1)
                {
                    builder.Append(credentialPath, index + 1, closingIndex - index - 1);
                    index = closingIndex + 1;
                    continue;
                }
            }

            if (character == '.')
            {
                builder.Append(ConfigurationPath.KeyDelimiter);
            }
            else
            {
                builder.Append(character);
            }

            index++;
        }

        return builder.ToString();
    }

    [LoggerMessage(Level = LogLevel.Warning,
        Message = "CredHub binding '{BindingName}' overwrites configuration key '{Key}', which was already set from a different source.")]
    private partial void LogOverwritingConfigurationKey(string key, string bindingName);
}
