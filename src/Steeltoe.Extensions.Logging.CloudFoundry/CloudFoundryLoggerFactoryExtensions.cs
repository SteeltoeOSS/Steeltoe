//
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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace Steeltoe.Extensions.Logging.CloudFoundry
{
    public static class CloudFoundryLoggerFactoryExtensions
    {
        public static ILoggerFactory AddCloudFoundry(this ILoggerFactory factory)
        {
            return factory.AddCloudFoundry(includeScopes: false);
        }

        public static ILoggerFactory AddCloudFoundry(this ILoggerFactory factory, bool includeScopes)
        {
            factory.AddCloudFoundry((n, l) => l >= LogLevel.Information, includeScopes);
            return factory;
        }

        public static ILoggerFactory AddCloudFoundry(this ILoggerFactory factory, LogLevel minLevel)
        {
            factory.AddCloudFoundry(minLevel, includeScopes: false);
            return factory;
        }

        public static ILoggerFactory AddCloudFoundry(
            this ILoggerFactory factory,
            LogLevel minLevel,
            bool includeScopes)
        {
            factory.AddCloudFoundry((category, logLevel) => logLevel >= minLevel, includeScopes);
            return factory;
        }

        public static ILoggerFactory AddCloudFoundry(
            this ILoggerFactory factory,
            Func<string, LogLevel, bool> filter)
        {
            factory.AddCloudFoundry(filter, includeScopes: false);
            return factory;
        }

        public static ILoggerFactory AddCloudFoundry(
            this ILoggerFactory factory,
            Func<string, LogLevel, bool> filter,
            bool includeScopes)
        {
            // factory.AddProvider(new CloudFoundryLoggerProvider(filter, includeScopes));
            factory.AddProvider(CloudFoundryLoggerProvider.CreateSingleton(filter, includeScopes));
            return factory;
        }

        public static ILoggerFactory AddCloudFoundry(
            this ILoggerFactory factory,
            ICloudFoundryLoggerSettings settings)
        {
            //factory.AddProvider(new CloudFoundryLoggerProvider(settings));
            factory.AddProvider(CloudFoundryLoggerProvider.CreateSingleton(settings));
            return factory;
        }

        public static ILoggerFactory AddCloudFoundry(this ILoggerFactory factory, IConfiguration configuration)
        {
            var settings = new CloudFoundryLoggerSettings(configuration);
            return factory.AddCloudFoundry(settings);
        }
    }
}
