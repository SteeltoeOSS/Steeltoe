// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Options;

//TODO rename to ManagementContextOptions
// It should hold settings for base level
public class ManagementEndpointOptions //: IManagementOptions
{


    public bool? Enabled { get; set; }

    public bool? Sensitive { get; set; }

    public string Path { get; set; }

    public string Port { get; set; }

    //public List<IEndpointOptions> EndpointOptions { get; set; }

    public bool UseStatusCodeFromResponse { get; set; } = true;

    private JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    public JsonSerializerOptions SerializerOptions { get {
            return _jsonOptions;
        } set
        {
            _jsonOptions = value;
        }
    } 
    
    public Exposure Exposure { get; set; } = new();
    
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
    }

}
