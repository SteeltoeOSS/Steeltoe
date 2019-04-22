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
using Microsoft.Extensions.Logging.Console;
using Owin;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.EndpointOwin.Test;
using System.Collections.Generic;

namespace Steeltoe.Management.EndpointOwin.Loggers.Test
{
    public class Startup
    {
        public static ILoggerFactory LoggerFactory;
        public static ILoggerProvider LoggerProvider;

        public void Configuration(IAppBuilder app)
        {
            var builder = new ConfigurationBuilder();

            var appSettings = new Dictionary<string, string>(OwinTestHelpers.Appsettings)
            {
            };

            builder.AddInMemoryCollection(appSettings);
            builder.AddEnvironmentVariables();
            var config = builder.Build();

            LoggerProvider = new DynamicLoggerProvider(new ConsoleLoggerSettings().FromConfiguration(config));
            LoggerFactory = new LoggerFactory();
            LoggerFactory.AddProvider(LoggerProvider);

            app.UseLoggersActuator(config, LoggerProvider, LoggerFactory);
        }
    }
}
