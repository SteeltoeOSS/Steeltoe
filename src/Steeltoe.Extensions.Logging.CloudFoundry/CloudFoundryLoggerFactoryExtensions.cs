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
