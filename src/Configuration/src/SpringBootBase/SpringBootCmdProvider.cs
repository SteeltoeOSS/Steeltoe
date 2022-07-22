// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace Steeltoe.Extensions.Configuration.SpringBoot;

/// <summary>
/// Configuration provider that expands Cmd Line '.' delimited configuration key/value pairs that start with "spring." to .NET compatible form
/// </summary>
public class SpringBootCmdProvider : ConfigurationProvider
{
    internal IConfiguration _config;
    private const string _keyPrefix = "spring.";

    /// <summary>
    /// Initializes a new instance of the <see cref="SpringBootCmdProvider"/> class.
    /// The <see cref="Configuration"/> will be created from the CommandLineConfigurationProvider.
    /// </summary>
    /// <param name="config">The Default CommandLineConfigurationProvider </param>
    public SpringBootCmdProvider(IConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public override void Load()
    {
        var enumerable = _config.AsEnumerable();
        foreach (var kvp in enumerable)
        {
            var key = kvp.Key;
            if (key.StartsWith(_keyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var nk = key.Replace('.', ':');
                Data[nk] = kvp.Value;
            }
        }
    }
}