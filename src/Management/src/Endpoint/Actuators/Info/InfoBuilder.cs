// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Actuators.Info;

public sealed class InfoBuilder : IInfoBuilder
{
    private readonly Dictionary<string, object> _info = new();

    public IDictionary<string, object> Build()
    {
        return _info;
    }

    public IInfoBuilder WithInfo(string key, object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _info[key] = value;

        return this;
    }

    public IInfoBuilder WithInfo(IDictionary<string, object> details)
    {
        ArgumentNullException.ThrowIfNull(details);

        foreach (KeyValuePair<string, object> pair in details)
        {
            _info[pair.Key] = pair.Value;
        }

        return this;
    }
}
