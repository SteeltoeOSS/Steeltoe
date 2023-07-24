// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Management.Info;

namespace Steeltoe.Management.Endpoint.Info.Contributor;

internal sealed class GitInfoContributor : ConfigurationContributor, IInfoContributor
{
    private const string GitSettingsPrefix = "git";
    private const string GitPropertiesFile = "git.properties";

    private static readonly List<string> DatetimeInputKeys = new()
    {
        "time"
    };

    private readonly string _propFile;
    private readonly ILogger _logger;

    public GitInfoContributor(ILogger<GitInfoContributor> logger)
        : this($"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}{GitPropertiesFile}", logger)
    {
    }

    public GitInfoContributor(string propFile, ILogger<GitInfoContributor> logger)
    {
        ArgumentGuard.NotNull(logger);

        _propFile = propFile;
        _logger = logger;
    }

    public async Task ContributeAsync(IInfoBuilder builder, CancellationToken cancellationToken)
    {
        ArgumentGuard.NotNull(builder);

        Configuration = await ReadGitPropertiesAsync(_propFile, cancellationToken);
        Contribute(builder, GitSettingsPrefix, true);
    }

    public async Task<IConfiguration> ReadGitPropertiesAsync(string propertiesPath, CancellationToken cancellationToken)
    {
        if (File.Exists(propertiesPath))
        {
            string[] lines = await File.ReadAllLinesAsync(propertiesPath, cancellationToken);

            if (lines.Length > 0)
            {
                var dictionary = new Dictionary<string, string>();

                foreach (string line in lines)
                {
                    if (line.StartsWith('#') || !line.StartsWith("git.", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string[] keyValuePair = line.Split('=');

                    if (keyValuePair.Length != 2)
                    {
                        continue;
                    }

                    string key = keyValuePair[0].Trim().Replace('.', ':');
                    string value = keyValuePair[1].Replace("\\:", ":", StringComparison.Ordinal);

                    dictionary[key] = value;
                }

                var builder = new ConfigurationBuilder();
                builder.AddInMemoryCollection(dictionary);
                return builder.Build();
            }
        }
        else
        {
            _logger.LogWarning("Unable to locate GitInfo at {GitInfoLocation}", propertiesPath);
        }

        return null;
    }

    protected override void AddKeyValue(IDictionary<string, object> dictionary, string key, string value)
    {
        ArgumentGuard.NotNull(dictionary);

        object valueToInsert = value;

        if (DatetimeInputKeys.Contains(key))
        {
            // Normalize datetime values to ISO8601 format
            valueToInsert = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        }

        dictionary[key] = valueToInsert;
    }
}
