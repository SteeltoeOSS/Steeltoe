// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration.Json;

namespace Steeltoe.Extensions.Configuration.SpringBoot;

/// <summary>
/// Configuration provider that expands the contents of SPRING_APPLICATION_JSON's Spring-style '.' delimited configuration key/value pairs to .NET compatible form.
/// </summary>
public class SpringBootEnvProvider : JsonStreamConfigurationProvider
{
    private const string SpringApplicationJson = "SPRING_APPLICATION_JSON";
    private readonly string _springApplicationJson;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpringBootEnvProvider"/> class.
    /// </summary>
    /// <param name="springApplicationJson"> The Json string to parse. </param>
    public SpringBootEnvProvider(string springApplicationJson = null)
        : base(new JsonStreamConfigurationSource())
    {
        _springApplicationJson = springApplicationJson;
    }

    /// <summary>
    /// Maps SPRING_APPLICATION_JSON into key:value pairs.
    /// </summary>
    public override void Load()
    {
        var json = _springApplicationJson ?? Environment.GetEnvironmentVariable(SpringApplicationJson);
        if (!string.IsNullOrEmpty(json))
        {
            Source.Stream = GetMemoryStream(json);
            base.Load();
            var keys = new List<string>(Data.Keys);
            foreach (var key in keys)
            {
                var value = Data[key];
                if (key.Contains('.') && value != null)
                {
                    var nk = key.Replace('.', ':');
                    Data[nk] = value;
                }
            }
        }
    }

    internal static MemoryStream GetMemoryStream(string json)
    {
        var memStream = new MemoryStream();
        var textWriter = new StreamWriter(memStream);
        textWriter.Write(json);
        textWriter.Flush();
        memStream.Seek(0, SeekOrigin.Begin);
        return memStream;
    }
}
