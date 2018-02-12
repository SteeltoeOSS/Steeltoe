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

using Steeltoe.CloudFoundry.Connector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Steeltoe.CloudFoundry.ConnectorBase.Cache
{
    public static class RedisTypeLocator
    {
        private static string[] msftRedisAssemblies = new string[] { "Microsoft.Extensions.Caching.Abstractions", "Microsoft.Extensions.Caching.Redis" };
        private static string[] msftRedisInterfaceTypeNames = new string[] { "Microsoft.Extensions.Caching.Distributed.IDistributedCache" };
        private static string[] msftRedisImplementationTypeNames = new string[] { "Microsoft.Extensions.Caching.Redis.RedisCache" };
        private static string[] msftRedisOptionNames = new string[] { "Microsoft.Extensions.Caching.Redis.RedisCacheOptions" };
        private static string[] stackExchangeRedisAssemblies = new string[] { "StackExchange.Redis", "StackExchange.Redis.StrongName" };
        private static string[] stackExchangeRedisInterfaceTypeNames = new string[] { "StackExchange.Redis.IConnectionMultiplexer" };
        private static string[] stackExchangeRedisImplementationTypeNames = new string[] { "StackExchange.Redis.ConnectionMultiplexer" };
        private static string[] stackExchangeRedisOptionNames = new string[] { "StackExchange.Redis.ConfigurationOptions" };

        public static Type MicrosoftRedisInterface => ConnectorHelpers.FindType(msftRedisAssemblies, msftRedisInterfaceTypeNames);

        public static Type MicrosoftRedisImplementation => ConnectorHelpers.FindType(msftRedisAssemblies, msftRedisImplementationTypeNames);

        public static Type MicrosoftRedisOptions => ConnectorHelpers.FindType(msftRedisAssemblies, msftRedisOptionNames);

        public static Type StackExchangeRedisInterface => ConnectorHelpers.FindType(stackExchangeRedisAssemblies, stackExchangeRedisInterfaceTypeNames);

        public static Type StackExchangeRedisImplementation => ConnectorHelpers.FindType(stackExchangeRedisAssemblies, stackExchangeRedisImplementationTypeNames);

        public static Type StackExchangeRedisOptions => ConnectorHelpers.FindType(stackExchangeRedisAssemblies, stackExchangeRedisOptionNames);

        public static MethodInfo StackExchangeInitializer =>
            ConnectorHelpers.FindMethod(StackExchangeRedisImplementation, "Connect", new Type[] { StackExchangeRedisOptions, typeof(TextWriter) });
    }
}
