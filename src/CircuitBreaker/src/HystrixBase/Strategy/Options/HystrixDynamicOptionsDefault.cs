// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

public class HystrixDynamicOptionsDefault : IHystrixDynamicOptions
{
    private readonly IConfiguration _configSource;

    public HystrixDynamicOptionsDefault(IConfiguration configSource)
    {
        _configSource = configSource;
    }

    public bool GetBoolean(string name, bool fallback)
    {
        var val = _configSource[name];
        if (val == null)
        {
            return fallback;
        }

        if (bool.TryParse(val, out var result))
        {
            return result;
        }

        return fallback;
    }

    public int GetInteger(string name, int fallback)
    {
        var val = _configSource[name];
        if (val == null)
        {
            return fallback;
        }

        if (int.TryParse(val, out var result))
        {
            return result;
        }

        return fallback;
    }

    public long GetLong(string name, long fallback)
    {
        var val = _configSource[name];
        if (val == null)
        {
            return fallback;
        }

        if (long.TryParse(val, out var result))
        {
            return result;
        }

        return fallback;
    }

    public string GetString(string name, string fallback)
    {
        var val = _configSource[name];
        if (val == null)
        {
            return fallback;
        }

        return val;
    }
}