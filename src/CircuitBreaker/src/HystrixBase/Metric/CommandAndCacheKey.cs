// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric;

public class CommandAndCacheKey
{
    private readonly string _commandName;
    private readonly string _cacheKey;

    public CommandAndCacheKey(string commandName, string cacheKey)
    {
        _commandName = commandName;
        _cacheKey = cacheKey;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj is not CommandAndCacheKey other || GetType() != obj.GetType())
        {
            return false;
        }

        return _commandName == other._commandName && _cacheKey == other._cacheKey;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_commandName, _cacheKey);
    }

    public override string ToString()
    {
        return $"CommandAndCacheKey{{commandName='{_commandName}', cacheKey='{_cacheKey}'}}";
    }
}
