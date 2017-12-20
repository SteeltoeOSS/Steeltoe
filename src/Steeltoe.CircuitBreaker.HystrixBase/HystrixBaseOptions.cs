// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
