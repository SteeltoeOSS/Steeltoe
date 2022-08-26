// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Steeltoe.Common;

namespace Steeltoe.Extensions.Configuration.SpringBoot;

/// <summary>
/// Configuration provider that expands command-line '.' delimited configuration key/value pairs that start with "spring." to .NET compatible form.
/// </summary>
public sealed class SpringBootCommandLineProvider : ConfigurationProvider
{
    private const string KeyPrefix = "spring.";
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpringBootCommandLineProvider" /> class. The <see cref="Configuration" /> will be created from the
    /// <see cref="CommandLineConfigurationProvider" />.
    /// </summary>
    /// <param name="configuration">
    /// The configuration.
    /// </param>
    public SpringBootCommandLineProvider(IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        _configuration = configuration;
    }

    public override void Load()
    {
        foreach (KeyValuePair<string, string> pair in _configuration.AsEnumerable())
        {
            string key = pair.Key;

            if (key.StartsWith(KeyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                string newKey = key.Replace('.', ':');
                Data[newKey] = pair.Value;
            }
        }
    }
}
