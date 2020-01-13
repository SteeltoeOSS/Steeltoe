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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Logging.DynamicLogger.Test
{
    public class DynamicLoggerHostBuilderExtensionsTest
    {
        [Fact]
        public void AddDynamicLogging_IHostBuilder_AddsDynamicLogging()
        {
            // Arrange
            var hostBuilder = new HostBuilder().AddDynamicLogging();

            // Act
            var host = hostBuilder.Build();
            var loggerProviders = host.Services.GetServices<ILoggerProvider>();

            // Assert
            Assert.Single(loggerProviders);
            Assert.IsType<DynamicConsoleLoggerProvider>(loggerProviders.First());
        }

        [Fact]
        public void AddDynamicLogging_IHostBuilder_RemovesConsoleLogging()
        {
            // Arrange
            var hostBuilder = new HostBuilder()
                .ConfigureLogging(ilb => ilb.AddConsole())
                .AddDynamicLogging();

            // Act
            var host = hostBuilder.Build();
            var loggerProviders = host.Services.GetServices<ILoggerProvider>();

            // Assert
            Assert.Single(loggerProviders);
            Assert.IsType<DynamicConsoleLoggerProvider>(loggerProviders.First());
        }

        [Fact]
        public void AddDynamicLogging_IHostBuilder_RemovesConsoleLoggingDefaultBuilder()
        {
            // Arrange
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureLogging(ilb => ilb.AddConsole())
                .AddDynamicLogging();

            // Act
            var host = hostBuilder.Build();
            var loggerProviders = host.Services.GetServices<ILoggerProvider>();

            // Assert
            Assert.DoesNotContain(loggerProviders, lp => lp is ConsoleLoggerProvider);
            Assert.Contains(loggerProviders, lp => lp is DynamicConsoleLoggerProvider);
        }
    }
}
