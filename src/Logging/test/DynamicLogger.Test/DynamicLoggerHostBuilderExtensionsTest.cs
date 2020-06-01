// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

#if NETCOREAPP3_0
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
#endif
    }
}
