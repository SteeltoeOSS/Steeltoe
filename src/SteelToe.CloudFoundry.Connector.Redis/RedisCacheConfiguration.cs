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

using Microsoft.Extensions.Configuration;
using System;


namespace SteelToe.CloudFoundry.Connector.Redis
{
    public class RedisCacheConfiguration : AbstractServiceConfiguration
    {
        public const string Default_Host = "localhost";
        public const int Default_Port = 6379;
        private const string REDIS_CLIENT_SECTION_PREFIX = "redis:client";

        public RedisCacheConfiguration()
        {
        }

        public RedisCacheConfiguration(IConfiguration config) :
            base()
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            var section = config.GetSection(REDIS_CLIENT_SECTION_PREFIX);
            section.Bind(this);
        }
    
        public string Host { get; set; } = Default_Host;
        public int Port { get; set; } = Default_Port;
        public string Password { get; set; }
        public string InstanceId { get; set; }
        public string ConnectionString { get; set; }
    }
}
