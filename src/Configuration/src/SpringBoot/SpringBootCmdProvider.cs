// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.SpringBoot;

/// <summary>
/// Configuration provider that expands Cmd Line '.' delimited configuration key/value pairs that start with "spring." to .NET compatible form.
/// </summary>
public class SpringBootCmdProvider : ConfigurationProvider
{
    private const string KeyPrefix = "spring.";
    internal IConfiguration Config;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpringBootCmdProvider" /> class. The <see cref="Configuration" /> will be created from the
    /// CommandLineConfigurationProvider.
    /// </summary>
    /// <param name="config">
    /// The Default CommandLineConfigurationProvider.
    /// </param>
    public SpringBootCmdProvider(IConfiguration config)
    {
        ArgumentGuard.NotNull(config);

        Config = config;
    }

    public override void Load()
    {
        IEnumerable<KeyValuePair<string, string>> enumerable = Config.AsEnumerable();

        foreach (KeyValuePair<string, string> kvp in enumerable)
        {
            string key = kvp.Key;

            if (key.StartsWith(KeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string nk = key.Replace('.', ':');
                Data[nk] = kvp.Value;
            }
        }
    }
}
