// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#pragma warning disable S4004 // Collection properties should be readonly
// https://github.com/dotnet/runtime/issues/30258

namespace Steeltoe.Extensions.Configuration.ConfigServer;

public sealed class ConfigEnvironment
{
    public string Name { get; set; }

    public string Label { get; set; }

    public IList<string> Profiles { get; set; }

    public IList<PropertySource> PropertySources { get; set; }

    public string Version { get; set; }

    public string State { get; set; }
}
