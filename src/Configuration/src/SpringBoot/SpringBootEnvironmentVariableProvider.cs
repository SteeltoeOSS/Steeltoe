// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration.Json;

namespace Steeltoe.Configuration.SpringBoot;

/// <summary>
/// Configuration provider that expands the contents of the SPRING_APPLICATION_JSON environment variable, using Spring-style dot-separated configuration
/// key/value pairs to .NET compatible form.
/// </summary>
internal sealed class SpringBootEnvironmentVariableProvider : JsonStreamConfigurationProvider
{
    private const string SpringApplicationJson = "SPRING_APPLICATION_JSON";
    private readonly string? _springApplicationJson;
    private bool _loaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpringBootEnvironmentVariableProvider" /> class that reads from the SPRING_APPLICATION_JSON environment
    /// variable.
    /// </summary>
    public SpringBootEnvironmentVariableProvider()
        : base(new JsonStreamConfigurationSource())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpringBootEnvironmentVariableProvider" /> class from the specified JSON.
    /// </summary>
    /// <param name="springApplicationJson">
    /// The Spring Application JSON.
    /// </param>
    public SpringBootEnvironmentVariableProvider(string springApplicationJson)
        : base(new JsonStreamConfigurationSource())
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(springApplicationJson);

        _springApplicationJson = springApplicationJson;
    }

    /// <summary>
    /// Maps the contents of the SPRING_APPLICATION_JSON environment variable into key:value pairs.
    /// </summary>
    public override void Load()
    {
        if (_loaded)
        {
            return;
        }

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        string? json = _springApplicationJson ?? Environment.GetEnvironmentVariable(SpringApplicationJson);

        if (!string.IsNullOrEmpty(json))
        {
            Source.Stream = GetStream(json);
            base.Load();

            foreach (string key in Data.Keys)
            {
                string? value = Data[key];

                if (value != null)
                {
                    string newKey = key.Contains('.') ? key.Replace('.', ':') : key;
                    data[newKey] = value;
                }
            }
        }

        Data = data;
        _loaded = true;
    }

    private static MemoryStream GetStream(string json)
    {
        var stream = new MemoryStream();

        using (var textWriter = new StreamWriter(stream, leaveOpen: true))
        {
            textWriter.Write(json);
        }

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }
}
