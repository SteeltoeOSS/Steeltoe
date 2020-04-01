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

using Steeltoe.Common.Reflection;
using System;
using System.IO;
using System.Reflection;

namespace Steeltoe.Connector.Redis
{
    public static class RedisTypeLocator
    {
        public static string[] MicrosoftAssemblies { get; internal set; } = new string[] { "Microsoft.Extensions.Caching.Abstractions", "Microsoft.Extensions.Caching.Redis", "Microsoft.Extensions.Caching.StackExchangeRedis" };

        public static string[] MicrosoftInterfaceTypeNames { get; internal set; } = new string[] { "Microsoft.Extensions.Caching.Distributed.IDistributedCache" };

        public static string[] MicrosoftImplementationTypeNames { get; internal set; } = new string[] { "Microsoft.Extensions.Caching.Redis.RedisCache", "Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache" };

        public static string[] MicrosoftOptionNames { get; internal set; } = new string[] { "Microsoft.Extensions.Caching.Redis.RedisCacheOptions", "Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions" };

        public static string[] StackExchangeAssemblies { get; internal set; } = new string[] { "StackExchange.Redis", "StackExchange.Redis.StrongName" };

        public static string[] StackExchangeInterfaceTypeNames { get; internal set; } = new string[] { "StackExchange.Redis.IConnectionMultiplexer" };

        public static string[] StackExchangeImplementationTypeNames { get; internal set; } = new string[] { "StackExchange.Redis.ConnectionMultiplexer" };

        public static string[] StackExchangeOptionNames { get; internal set; } = new string[] { "StackExchange.Redis.ConfigurationOptions" };

        public static string[] StackExchangeCommandFlagsNamesValue { get; internal set; } = new string[] { "StackExchange.Redis.CommandFlags" };

        /// <summary>
        /// Gets IDistributedCache from a Microsoft Cache library
        /// </summary>
        public static Type MicrosoftInterface => ReflectionHelpers.FindTypeOrThrow(MicrosoftAssemblies, MicrosoftInterfaceTypeNames, MicrosoftInterfaceTypeNames[0], "a Microsoft Caching NuGet Reference");

        /// <summary>
        /// Gets RedisCache from a Microsoft Cache library
        /// </summary>
        public static Type MicrosoftImplementation => ReflectionHelpers.FindTypeOrThrow(MicrosoftAssemblies, MicrosoftImplementationTypeNames, "RedisCache", "a Microsoft Caching NuGet Reference");

        /// <summary>
        /// Gets RedisCacheOptions from a Microsoft Cache library
        /// </summary>
        public static Type MicrosoftOptions => ReflectionHelpers.FindTypeOrThrow(MicrosoftAssemblies, MicrosoftOptionNames, MicrosoftOptionNames[0], "a Microsoft Caching NuGet Reference");

        /// <summary>
        /// Gets IConnectionMultiplexer from a StackExchange Redis library
        /// </summary>
        public static Type StackExchangeInterface => ReflectionHelpers.FindTypeOrThrow(StackExchangeAssemblies, StackExchangeInterfaceTypeNames, StackExchangeInterfaceTypeNames[0], "a Stack Exchange Redis NuGet Reference");

        /// <summary>
        /// Gets ConnectionMultiplexer from a StackExchange Redis library
        /// </summary>
        public static Type StackExchangeImplementation => ReflectionHelpers.FindTypeOrThrow(StackExchangeAssemblies, StackExchangeImplementationTypeNames, StackExchangeImplementationTypeNames[0], "a Stack Exchange Redis NuGet Reference");

        /// <summary>
        /// Gets CommandFlags from StackExchange Redis library
        /// </summary>
        public static Type StackExchangeCommandFlagsNames => ReflectionHelpers.FindTypeOrThrow(StackExchangeAssemblies, StackExchangeCommandFlagsNamesValue, StackExchangeCommandFlagsNamesValue[0], "a Stack Exchange Redis NuGet Reference");

        /// <summary>
        /// Gets ConfigurationOptions from a StackExchange Redis library
        /// </summary>
        public static Type StackExchangeOptions => ReflectionHelpers.FindTypeOrThrow(StackExchangeAssemblies, StackExchangeOptionNames, StackExchangeOptionNames[0], "a Stack Exchange Redis NuGet Reference");

        /// <summary>
        /// Gets the Connect method from a StackExchange Redis library
        /// </summary>
        public static MethodInfo StackExchangeInitializer => FindMethodOrThrow(StackExchangeImplementation, "Connect", new Type[] { StackExchangeOptions, typeof(TextWriter) });

        private static MethodInfo FindMethodOrThrow(Type type, string methodName, Type[] parameters = null)
        {
            var returnType = ReflectionHelpers.FindMethod(type, methodName, parameters);
            if (returnType == null)
            {
                throw new ConnectorException("Unable to find required Redis types, are you missing a Redis Nuget package?");
            }

            return returnType;
        }
    }
}
