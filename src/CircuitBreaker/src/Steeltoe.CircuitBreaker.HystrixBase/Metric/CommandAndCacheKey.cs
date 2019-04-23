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

using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric
{
    public class CommandAndCacheKey
    {
        private readonly string commandName;
        private readonly string cacheKey;

        public CommandAndCacheKey(string commandName, string cacheKey)
        {
            this.commandName = commandName;
            this.cacheKey = cacheKey;
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

            CommandAndCacheKey that = (CommandAndCacheKey)o;

            if (!commandName.Equals(that.commandName))
            {
                return false;
            }

            return cacheKey.Equals(that.cacheKey);
        }

        public override int GetHashCode()
        {
            int result = commandName.GetHashCode();
            result = (31 * result) + cacheKey.GetHashCode();
            return result;
        }

        public override string ToString()
        {
            return "CommandAndCacheKey{" +
                    "commandName='" + commandName + '\'' +
                    ", cacheKey='" + cacheKey + '\'' +
                    '}';
        }
    }
}
