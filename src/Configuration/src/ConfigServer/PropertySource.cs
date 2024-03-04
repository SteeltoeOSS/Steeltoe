// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.ConfigServer;

// https://github.com/dotnet/runtime/issues/30258
#pragma warning disable S4004 // Collection properties should be readonly

public sealed class PropertySource
{
    public string? Name { get; set; }
    public IDictionary<string, object> Source { get; set; } = new Dictionary<string, object>();
}
