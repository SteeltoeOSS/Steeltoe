// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.Json;

#pragma warning disable S4004 // Collection properties should be readonly

namespace Steeltoe.Management.Endpoint.Options;

public sealed class ManagementOptions
{
    internal bool IsCloudFoundryEnabled { get; set; }

    public bool? Enabled { get; set; }
    public string? Path { get; set; }
    public string? Port { get; set; }
    public bool UseStatusCodeFromResponse { get; set; } = true;

    public JsonSerializerOptions SerializerOptions { get; set; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Exposure? Exposure { get; set; } = new();

    /// <summary>
    /// Gets or sets a list of
    /// <see href="https://docs.microsoft.com/dotnet/api/system.type.assemblyqualifiedname">
    /// assembly-qualified
    /// </see>
    /// custom JsonConverters.
    /// </summary>
    public IList<string> CustomJsonConverters { get; set; } = new List<string>();
}
