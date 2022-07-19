// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

namespace Steeltoe.CircuitBreaker.Hystrix;

public abstract class HystrixBaseOptions
{
    protected internal IHystrixDynamicOptions Dynamic;

    protected HystrixBaseOptions(IHystrixDynamicOptions dynamicOptions)
    {
        Dynamic = dynamicOptions;
    }

    protected virtual bool GetBoolean(string prefix, string key, string property, bool globalDefault, bool? instanceDefaultFromCode)
    {
        var result = globalDefault;
        result = Dynamic?.GetBoolean($"{prefix}:default:{property}", result) ?? result; // dynamic global default
        result = instanceDefaultFromCode ?? result; // instance default from code
        result = Dynamic?.GetBoolean($"{prefix}:{key}:{property}", result) ?? result; // dynamic instance value
        return result;
    }

    protected virtual int GetInteger(string prefix, string key, string property, int globalDefault, int? instanceDefaultFromCode)
    {
        var result = globalDefault;
        result = Dynamic?.GetInteger($"{prefix}:default:{property}", result) ?? result; // dynamic global default
        result = instanceDefaultFromCode ?? result; // instance default from code
        result = Dynamic?.GetInteger($"{prefix}:{key}:{property}", result) ?? result; // dynamic instance value
        return result;
    }

    protected virtual long GetLong(string prefix, string key, string property, long globalDefault, long? instanceDefaultFromCode)
    {
        var result = globalDefault;
        result = Dynamic?.GetLong($"{prefix}:default:{property}", result) ?? result; // dynamic global default
        result = instanceDefaultFromCode ?? result; // instance default from code
        result = Dynamic?.GetLong($"{prefix}:{key}:{property}", result) ?? result; // dynamic instance value
        return result;
    }

    protected virtual string GetString(string prefix, string key, string property, string globalDefault, string instanceDefaultFromCode)
    {
        var result = globalDefault;
        result = Dynamic != null ? Dynamic.GetString($"{prefix}:default:{property}", result) : result; // dynamic global default
        result = instanceDefaultFromCode ?? result; // instance default from code
        result = Dynamic != null ? Dynamic.GetString($"{prefix}:{key}:{property}", result) : result; // dynamic instance value
        return result;
    }
}
