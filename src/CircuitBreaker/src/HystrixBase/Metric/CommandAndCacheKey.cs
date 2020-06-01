// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
