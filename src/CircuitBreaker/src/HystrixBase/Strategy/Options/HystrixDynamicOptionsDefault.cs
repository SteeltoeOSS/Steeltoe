// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Options
{
    public class HystrixDynamicOptionsDefault : IHystrixDynamicOptions
    {
        private readonly IConfiguration configSource;

        public HystrixDynamicOptionsDefault(IConfiguration configSource)
        {
            this.configSource = configSource;
        }

        public bool GetBoolean(string name, bool fallback)
        {
            var val = configSource[name];
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
            var val = configSource[name];
            if (val == null)
            {
                return fallback;
            }

            var result = -1;
            if (int.TryParse(val, out result))
            {
                return result;
            }

            return fallback;
        }

        public long GetLong(string name, long fallback)
        {
            var val = configSource[name];
            if (val == null)
            {
                return fallback;
            }

            long result = -1;
            if (long.TryParse(val, out result))
            {
                return result;
            }

            return fallback;
        }

        public string GetString(string name, string fallback)
        {
            var val = configSource[name];
            if (val == null)
            {
                return fallback;
            }

            return val;
        }
    }
}
