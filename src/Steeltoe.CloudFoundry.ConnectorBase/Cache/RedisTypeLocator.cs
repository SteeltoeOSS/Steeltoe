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

using System;
using System.IO;
using System.Reflection;

namespace Steeltoe.CloudFoundry.Connector.Redis
{
    public static class RedisTypeLocator
    {
        public static string[] MicrosoftAssemblies = new string[] { "Microsoft.Extensions.Caching.Abstractions", "Microsoft.Extensions.Caching.Redis" };
        public static string[] MicrosoftInterfaceTypeNames = new string[] { "Microsoft.Extensions.Caching.Distributed.IDistributedCache" };
        public static string[] MicrosoftImplementationTypeNames = new string[] { "Microsoft.Extensions.Caching.Redis.RedisCache" };
        public static string[] MicrosoftOptionNames = new string[] { "Microsoft.Extensions.Caching.Redis.RedisCacheOptions" };
        public static string[] StackExchangeAssemblies = new string[] { "StackExchange.Redis", "StackExchange.Redis.StrongName" };
        public static string[] StackExchangeInterfaceTypeNames = new string[] { "StackExchange.Redis.IConnectionMultiplexer" };
        public static string[] StackExchangeImplementationTypeNames = new string[] { "StackExchange.Redis.ConnectionMultiplexer" };
        public static string[] StackExchangeOptionNames = new string[] { "StackExchange.Redis.ConfigurationOptions" };
        public static string[] StackExchangeCommandFlagsNamesValue = new string[] { "StackExchange.Redis.CommandFlags" };

        /// <summary>
        /// Gets IDistributedCache from a Microsoft Cache library
        /// </summary>
        public static Type MicrosoftInterface => FindTypeOrThrow(MicrosoftAssemblies, MicrosoftInterfaceTypeNames);

        /// <summary>
        /// Gets RedisCache from a Microsoft Cache library
        /// </summary>
        public static Type MicrosoftImplementation => FindTypeOrThrow(MicrosoftAssemblies, MicrosoftImplementationTypeNames);

        /// <summary>
        /// Gets RedisCacheOptions from a Microsoft Cache library
        /// </summary>
        public static Type MicrosoftOptions => FindTypeOrThrow(MicrosoftAssemblies, MicrosoftOptionNames);

        /// <summary>
        /// Gets IConnectionMultiplexer from a StackExchange Redis library
        /// </summary>
        public static Type StackExchangeInterface => FindTypeOrThrow(StackExchangeAssemblies, StackExchangeInterfaceTypeNames);

        /// <summary>
        /// Gets ConnectionMultiplexer from a StackExchange Redis library
        /// </summary>
        public static Type StackExchangeImplementation => FindTypeOrThrow(StackExchangeAssemblies, StackExchangeImplementationTypeNames);

        /// <summary>
        /// Gets CommandFlags from StackExchange Redis library
        /// </summary>
        public static Type StackExchangeCommandFlagsNames => FindTypeOrThrow(StackExchangeAssemblies, StackExchangeCommandFlagsNamesValue);

        /// <summary>
        /// Gets ConfigurationOptions from a StackExchange Redis library
        /// </summary>
        public static Type StackExchangeOptions => FindTypeOrThrow(StackExchangeAssemblies, StackExchangeOptionNames);

        /// <summary>
        /// Gets the Connect method from a StackExchange Redis library
        /// </summary>
        public static MethodInfo StackExchangeInitializer => FindMethodOrThrow(StackExchangeImplementation, "Connect", new Type[] { StackExchangeOptions, typeof(TextWriter) });

        private static Type FindTypeOrThrow(string[] assemblies, string[] types)
        {
            var type = ConnectorHelpers.FindType(assemblies, types);
            if (type == null)
            {
                throw new ConnectorException("Unable to find required Redis types, are you missing a Redis Nuget package?");
            }

            return type;
        }

        private static MethodInfo FindMethodOrThrow(Type type, string methodName, Type[] parameters = null)
        {
            var returnType = ConnectorHelpers.FindMethod(type, methodName, parameters);
            if (returnType == null)
            {
                throw new ConnectorException("Unable to find required Redis types, are you missing a Redis Nuget package?");
            }

            return returnType;
        }
    }
}
