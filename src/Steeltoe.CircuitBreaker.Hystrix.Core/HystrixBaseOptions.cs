using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public abstract class HystrixBaseOptions
    {

        internal protected IHystrixDynamicOptions dynamic;

        protected HystrixBaseOptions(IHystrixDynamicOptions dynamicOptions)
        {
            this.dynamic = dynamicOptions;
        }

        protected virtual bool GetBoolean(string prefix, string key, string property, bool globalDefault, bool? instanceDefaultFromCode)
        {
            bool result = globalDefault;
            result = (dynamic != null) ? dynamic.GetBoolean(prefix + ":default:" + property, result) : result; // dynamic global default
            result = instanceDefaultFromCode.HasValue ? instanceDefaultFromCode.Value : result; // instance default from code
            result = (dynamic != null) ? dynamic.GetBoolean(prefix + ":" + key + ":" + property, result) : result; // dynamic instance value
            return result;

        }
        protected virtual int GetInteger(string prefix, string key, string property, int globalDefault, int? instanceDefaultFromCode)
        {
            int result = globalDefault;
            result = (dynamic != null) ? dynamic.GetInteger(prefix + ":default:" + property, result) : result; // dynamic global default
            result = instanceDefaultFromCode.HasValue ? instanceDefaultFromCode.Value : result; // instance default from code
            result = (dynamic != null) ? dynamic.GetInteger(prefix + ":" + key + ":" + property, result) : result; // dynamic instance value
            return result;

        }
        protected virtual long GetLong(string prefix, string key, string property, long globalDefault, long? instanceDefaultFromCode)
        {
            long result = globalDefault;
            result = (dynamic != null) ? dynamic.GetLong(prefix + ":default:" + property, result) : result; // dynamic global default
            result = instanceDefaultFromCode.HasValue ? instanceDefaultFromCode.Value : result; // instance default from code
            result = (dynamic != null) ? dynamic.GetLong(prefix + ":" + key + ":" + property, result) : result; // dynamic instance value
            return result;

        }
        protected virtual string GetString(string prefix, string key, string property, string globalDefault, string instanceDefaultFromCode)
        {
            string result = globalDefault;
            result = (dynamic != null) ? dynamic.GetString(prefix + ":default:" + property, result) : result; // dynamic global default
            result = instanceDefaultFromCode != null ? instanceDefaultFromCode : result; // instance default from code
            result = (dynamic != null) ? dynamic.GetString(prefix + ":" + key + ":" + property, result) : result; // dynamic instance value
            return result;

        }
    }
}
