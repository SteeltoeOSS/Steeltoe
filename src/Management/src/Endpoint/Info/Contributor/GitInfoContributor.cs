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
        : this(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + GitPropertiesFile, logger)
    {
    }

    public GitInfoContributor(string propFile, ILogger<GitInfoContributor> logger)
    {
        ArgumentGuard.NotNull(logger);

        _propFile = propFile;
        _logger = logger;
    }

    public async Task ContributeAsync(IInfoBuilder builder)
    {
        Configuration = await ReadGitPropertiesAsync(_propFile);
        Contribute(builder, GitSettingsPrefix, true);
    }

    public async Task<IConfiguration> ReadGitPropertiesAsync(string propFile)
    {
        if (File.Exists(propFile))
        {
            string[] lines = await File.ReadAllLinesAsync(propFile);

            if (lines != null && lines.Length > 0)
            {
                var dict = new Dictionary<string, string>();

                foreach (string line in lines)
                {
                    if (line.StartsWith('#') || !line.StartsWith("git.", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string[] keyVal = line.Split('=');

                    if (keyVal == null || keyVal.Length != 2)
                    {
                        continue;
                    }

                    string key = keyVal[0].Trim().Replace('.', ':');
                    string val = keyVal[1].Replace("\\:", ":", StringComparison.Ordinal);

                    dict[key] = val;
                }

                var builder = new ConfigurationBuilder();
                builder.AddInMemoryCollection(dict);
                return builder.Build();
            }
        }
        else
        {
            _logger.LogWarning("Unable to locate GitInfo at {GitInfoLocation}", propFile);
        }

        return null;
    }

    protected override void AddKeyValue(Dictionary<string, object> dict, string key, string value)
    {
        ArgumentGuard.NotNull(dict);

        object valueToInsert = value;

        if (DatetimeInputKeys.Contains(key))
        {
            // Normalize datetime values to ISO8601 format
            valueToInsert = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        }

        dict[key] = valueToInsert;
    }
}
