// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.DataProtection.StackExchangeRedis;
using StackExchange.Redis;

namespace Steeltoe.Security.DataProtection.Redis
{
    public class CloudFoundryRedisXmlRepository : RedisXmlRepository
    {
        private const string DataProtectionKeysName = "DataProtection-Keys";

        public CloudFoundryRedisXmlRepository(IConnectionMultiplexer redis)
            : base(() => redis.GetDatabase(), DataProtectionKeysName)
        {
        }
    }
}
