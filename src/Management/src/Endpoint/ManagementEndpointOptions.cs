// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint;

public class ManagementEndpointOptions : IManagementOptions
{
    private const string DefaultPath = "/actuator";
    private const string ManagementInfoPrefix = "management:endpoints";

    public bool? Enabled { get; set; }

    public bool? Sensitive { get; set; }

    public string Path { get; set; }

    public List<IEndpointOptions> EndpointOptions { get; set; }

    public bool UseStatusCodeFromResponse { get; set; } = true;

    public JsonSerializerOptions SerializerOptions { get; set; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Gets or sets a list of
    /// <see href="https://docs.microsoft.com/dotnet/api/system.type.assemblyqualifiedname">
    /// assembly-qualified
    /// </see>
    /// custom JsonConverters.
    /// </summary>
    public string[] CustomJsonConverters { get; set; }

    public ManagementEndpointOptions()
    {
        Path = DefaultPath;
        EndpointOptions = new List<IEndpointOptions>();
    }

    public ManagementEndpointOptions(IConfiguration config)
        : this()
    {
        ArgumentGuard.NotNull(config);

        IConfigurationSection section = config.GetSection(ManagementInfoPrefix);

        if (section != null)
        {
            section.Bind(this);

            foreach (string converterTypeName in CustomJsonConverters ?? Array.Empty<string>())
            {
                var converterType = Type.GetType(converterTypeName);

                if (converterType != null)
                {
                    var converterInstance = (JsonConverter)Activator.CreateInstance(converterType);
                    SerializerOptions.Converters.Add(converterInstance);
                }
            }
        }
    }
}
