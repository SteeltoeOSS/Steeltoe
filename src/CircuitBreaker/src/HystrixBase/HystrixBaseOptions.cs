// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public abstract class HystrixBaseOptions
    {
        protected internal IHystrixDynamicOptions _dynamic;

        protected HystrixBaseOptions(IHystrixDynamicOptions dynamicOptions)
        {
            this._dynamic = dynamicOptions;
        }

        protected virtual bool GetBoolean(string prefix, string key, string property, bool globalDefault, bool? instanceDefaultFromCode)
        {
            bool result = globalDefault;
            result = (_dynamic != null) ? _dynamic.GetBoolean(prefix + ":default:" + property, result) : result; // dynamic global default
            result = instanceDefaultFromCode ?? result; // instance default from code
            result = (_dynamic != null) ? _dynamic.GetBoolean(prefix + ":" + key + ":" + property, result) : result; // dynamic instance value
            return result;
        }

        protected virtual int GetInteger(string prefix, string key, string property, int globalDefault, int? instanceDefaultFromCode)
        {
            int result = globalDefault;
            result = (_dynamic != null) ? _dynamic.GetInteger(prefix + ":default:" + property, result) : result; // dynamic global default
            result = instanceDefaultFromCode ?? result; // instance default from code
            result = (_dynamic != null) ? _dynamic.GetInteger(prefix + ":" + key + ":" + property, result) : result; // dynamic instance value
            return result;
        }

        protected virtual long GetLong(string prefix, string key, string property, long globalDefault, long? instanceDefaultFromCode)
        {
            long result = globalDefault;
            result = (_dynamic != null) ? _dynamic.GetLong(prefix + ":default:" + property, result) : result; // dynamic global default
            result = instanceDefaultFromCode ?? result; // instance default from code
            result = (_dynamic != null) ? _dynamic.GetLong(prefix + ":" + key + ":" + property, result) : result; // dynamic instance value
            return result;
        }

        protected virtual string GetString(string prefix, string key, string property, string globalDefault, string instanceDefaultFromCode)
        {
            string result = globalDefault;
            result = (_dynamic != null) ? _dynamic.GetString(prefix + ":default:" + property, result) : result; // dynamic global default
            result = instanceDefaultFromCode ?? result; // instance default from code
            result = (_dynamic != null) ? _dynamic.GetString(prefix + ":" + key + ":" + property, result) : result; // dynamic instance value
            return result;
        }
    }
}
