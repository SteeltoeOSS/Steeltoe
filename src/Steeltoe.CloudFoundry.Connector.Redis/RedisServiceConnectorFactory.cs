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

using System;

using Microsoft.Extensions.Caching.Redis;
using Steeltoe.CloudFoundry.Connector.Services;
using StackExchange.Redis;

namespace Steeltoe.CloudFoundry.Connector.Redis
{
    public class RedisServiceConnectorFactory
    {
        private RedisServiceInfo _info;
        private RedisCacheConnectorOptions _config;
        private RedisCacheConfigurer _configurer = new RedisCacheConfigurer();
        public RedisServiceConnectorFactory(RedisServiceInfo sinfo, RedisCacheConnectorOptions config)
        {
            _info = sinfo;
            _config = config;
        }
        internal RedisCache CreateCache(IServiceProvider provider)
        {
            var opts = _configurer.Configure(_info, _config);
            return new RedisCache(opts);
        }

        internal ConnectionMultiplexer CreateConnection(IServiceProvider provider)
        {
            var opts = _configurer.ConfigureConnection(_info, _config);
            return ConnectionMultiplexer.Connect(opts);
        }
    }
}
