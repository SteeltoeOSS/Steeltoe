// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.ConfigServer;

public sealed class PropertySource
{
    public string? Name { get; set; }
    public IDictionary<string, object> Source { get; } = new Dictionary<string, object>();
}
