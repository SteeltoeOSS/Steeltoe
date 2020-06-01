// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Owin;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.EndpointOwin.Test;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwin.Loggers.Test
{
    public class Startup
    {
        public static ILoggerFactory LoggerFactory;
        public static IDynamicLoggerProvider LoggerProvider;

        public void Configuration(IAppBuilder app)
        {
            var cfgBuilder = new ConfigurationBuilder();

            var appSettings = new Dictionary<string, string>(OwinTestHelpers.Appsettings)
            {
            };

            cfgBuilder.AddInMemoryCollection(appSettings);
            cfgBuilder.AddEnvironmentVariables();
            var config = cfgBuilder.Build();

            var services = new ServiceCollection()
                .AddLogging((logBuilder) =>
                {
                    logBuilder.AddConfiguration(config);
                    logBuilder.AddDynamicConsole();
                })
                .BuildServiceProvider();

            LoggerProvider = services.GetRequiredService<ILoggerProvider>() as DynamicConsoleLoggerProvider;
            LoggerFactory = services.GetRequiredService<ILoggerFactory>();

            app.UseLoggersActuator(config, LoggerProvider, LoggerFactory);
        }
    }
}
