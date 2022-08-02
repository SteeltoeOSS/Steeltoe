// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Extensions.Configuration.ConfigServer;

public class PropertySource
{
    public string Name { get; set; }

    public IDictionary<string, object> Source { get; set; }

    public PropertySource()
    {
    }

    public PropertySource(string name, IDictionary<string, object> properties)
    {
        Name = name;
        Source = properties;
    }
}
