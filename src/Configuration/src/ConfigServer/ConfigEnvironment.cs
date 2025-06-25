// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.ConfigServer;

public sealed class ConfigEnvironment
{
    public string? Name { get; set; }
    public string? Label { get; set; }
    public IList<string> Profiles { get; } = new List<string>();
    public IList<PropertySource> PropertySources { get; } = new List<PropertySource>();
    public string? Version { get; set; }
    public string? State { get; set; }
}
