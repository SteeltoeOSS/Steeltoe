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
