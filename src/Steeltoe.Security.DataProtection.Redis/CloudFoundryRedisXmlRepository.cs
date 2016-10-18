//
// Copyright 2015 the original author or authors.
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
//

using Microsoft.AspNetCore.DataProtection;
using StackExchange.Redis;

namespace Steeltoe.Security.DataProtection.Redis
{
    // Note: RedisXmlRepository has been copied "as is" from version 1.1 of Microsoft.AspNetCore.DataProtection.Redis.  When
    // Microsoft releases 1.1 remove RedisXmlRepository from this project and add reference to Microsoft.AspNetCore.DataProtection.Redis nuget.
    public class CloudFoundryRedisXmlRepository : RedisXmlRepository
    {
        private const string DataProtectionKeysName = "DataProtection-Keys";
        public CloudFoundryRedisXmlRepository(ConnectionMultiplexer redis) : base( () => redis.GetDatabase(), DataProtectionKeysName)
        {
        }
    }
}
