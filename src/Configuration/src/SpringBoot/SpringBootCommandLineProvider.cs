// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration.SpringBoot;

/// <summary>
/// Configuration provider that expands '.'-separated configuration keys that start with "spring." (typically originating from Spring Boot command-line
/// parameters) to .NET compatible form.
/// </summary>
internal sealed class SpringBootCommandLineProvider : ConfigurationProvider
{
    private const string KeyPrefix = "spring.";
    private readonly string[] _args;

    public SpringBootCommandLineProvider(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        _args = args;
    }

    public override void Load()
    {
        IConfigurationRoot? configurationRoot = null;

        try
        {
            configurationRoot = new ConfigurationBuilder().AddCommandLine(_args).Build();
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            foreach ((string key, string? value) in configurationRoot.AsEnumerable())
            {
                if (key.StartsWith(KeyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Convert array references, for example: foo:bar[1]:baz -> foo:bar:1:baz
                    string newKey = key.Replace('.', ':').Replace('[', ':').Replace("]", string.Empty, StringComparison.Ordinal);
                    data[newKey] = value;
                }
            }

            Data = data;
        }
        finally
        {
            if (configurationRoot is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
