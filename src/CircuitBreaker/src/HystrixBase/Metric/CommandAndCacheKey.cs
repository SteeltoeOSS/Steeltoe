// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

    public override bool Equals(object o)
    {
        if (this == o)
        {
            return true;
        }

        if (o == null || GetType() != o.GetType())
        {
            return false;
        }

        var that = (CommandAndCacheKey)o;

        if (!_commandName.Equals(that._commandName))
        {
            return false;
        }

        return _cacheKey.Equals(that._cacheKey);
    }

    public override int GetHashCode()
    {
        var result = _commandName.GetHashCode();
        result = (31 * result) + _cacheKey.GetHashCode();
        return result;
    }

    public override string ToString()
    {
        return "CommandAndCacheKey{" +
               "commandName='" + _commandName + '\'' +
               ", cacheKey='" + _cacheKey + '\'' +
               '}';
    }
}