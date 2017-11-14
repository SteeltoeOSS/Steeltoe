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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Extensions.Logging.Test
{
    public class DynamicLoggingBuilderTest
    {
        private static Dictionary<string, string> appsettings = new Dictionary<string, string>()
        {
            ["Logging:IncludeScopes"] = "false",
            ["Logging:Console:LogLevel:Default"] = "Information",
            ["Logging:Console:LogLevel:A.B.C.D"] = "Critical",
            ["Logging:LogLevel:Steeltoe.Extensions.Logging.Test"] = "Information",
            ["Logging:LogLevel:Default"] = "Warning"
        };

        [Fact]
        public void OnlyApplicableFilters_AreApplied()
        {
            // arrange
            var _appsettings = new Dictionary<string, string>()
            {
                ["Logging:IncludeScopes"] = "false",
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:foo:LogLevel:A.B.C.D.TestClass"] = "None"
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(_appsettings).Build();
            var services = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                    builder.AddDynamicConsole();
                })
                .BuildServiceProvider();

            // act
            var logger = services.GetService(typeof(ILogger<A.B.C.D.TestClass>)) as ILogger<A.B.C.D.TestClass>;

            // assert
            Assert.NotNull(logger);
            Assert.True((logger).IsEnabled(LogLevel.Information), "Information level should be enabled");
            Assert.False((logger).IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
        }

        [Fact]
        public void DynamicLevelSetting_WorksWith_ConsoleFilters()
        {
            // arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                    builder.AddDynamicConsole();
                })
                .BuildServiceProvider();

            // act
            var logger = services.GetService(typeof(ILogger<A.B.C.D.TestClass>)) as ILogger<A.B.C.D.TestClass>;

            // assert
            Assert.NotNull(logger);
            Assert.True((logger).IsEnabled(LogLevel.Critical), "Critical level should be enabled");
            Assert.False((logger).IsEnabled(LogLevel.Error), "Error level should NOT be enabled");
            Assert.False((logger).IsEnabled(LogLevel.Warning), "Warning level should NOT be enabled");
            Assert.False((logger).IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
            Assert.False((logger).IsEnabled(LogLevel.Trace), "Trace level should NOT be enabled yet");

            // change the log level and confirm it worked
            var provider = services.GetRequiredService(typeof(ILoggerProvider)) as DynamicLoggerProvider;
            provider.SetLogLevel("A.B.C.D", LogLevel.Trace);
            Assert.True((logger).IsEnabled(LogLevel.Trace), "Trace level should have been enabled");
        }

        [Fact]
        public void AddConsole_Works_WithAddConfiguration()
        {
            // arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                    builder.AddConsole();
                }).BuildServiceProvider();

            // act
            var logger = services.GetService(typeof(ILogger<DynamicLoggingBuilderTest>)) as ILogger<DynamicLoggingBuilderTest>;

            // assert
            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(LogLevel.Warning), "Warning level should be enabled");
            Assert.False(logger.IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
        }

        [Fact]
        public void AddDynamicConsole_Works_WithConfigurationParam()
        {
            // arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection()
                .AddLogging(builder => builder.AddDynamicConsole(configuration))
                .BuildServiceProvider();

            // act
            var logger = services.GetService(typeof(ILogger<DynamicLoggingBuilderTest>)) as ILogger<DynamicLoggingBuilderTest>;

            // assert
            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(LogLevel.Warning), "Warning level should be enabled");
            Assert.False(logger.IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
        }

        [Fact]
        public void AddDynamicConsole_Works_WithAddConfiguration()
        {
            // arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                    builder.AddDynamicConsole();
                }).BuildServiceProvider();

            // act
            var logger = services.GetService(typeof(ILogger<DynamicLoggingBuilderTest>)) as ILogger<DynamicLoggingBuilderTest>;

            // assert
            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(LogLevel.Warning), "Warning level should be enabled");
            Assert.False(logger.IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
        }

        [Fact]
        public void DynamicLevelSetting_ParmLessAddDynamic_NotBrokenByAddConfiguration()
        {
            // arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                    builder.AddDynamicConsole();
                })
                .BuildServiceProvider();

            // act
            var logger = services.GetService(typeof(ILogger<DynamicLoggingBuilderTest>)) as ILogger<DynamicLoggingBuilderTest>;

            // assert
            Assert.NotNull(logger);
            Assert.True((logger).IsEnabled(LogLevel.Warning), "Warning level should be enabled");
            Assert.False((logger).IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
            Assert.False((logger).IsEnabled(LogLevel.Trace), "Trace level should not be enabled yet");

            // change the log level and confirm it worked
            var provider = services.GetRequiredService(typeof(ILoggerProvider)) as DynamicLoggerProvider;
            provider.SetLogLevel("Steeltoe.Extensions.Logging.Test", LogLevel.Trace);
            Assert.True((logger).IsEnabled(LogLevel.Trace), "Trace level should have been enabled");
        }

        [Fact]
        public void DynamicLevelSetting_WithParmsAddDynamic_NotBrokenByAddConfiguration()
        {
            // arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConfiguration(configuration.GetSection("Logging"));
                    builder.AddDynamicConsole(configuration);
                })
                .BuildServiceProvider();

            // act
            var logger = services.GetService(typeof(ILogger<DynamicLoggingBuilderTest>)) as ILogger<DynamicLoggingBuilderTest>;

            // assert
            Assert.NotNull(logger);
            Assert.True((logger).IsEnabled(LogLevel.Warning), "Warning level should be enabled");
            Assert.False((logger).IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
            Assert.False((logger).IsEnabled(LogLevel.Trace), "Trace level should not be enabled yet");

            // change the log level and confirm it worked
            var provider = services.GetRequiredService(typeof(ILoggerProvider)) as DynamicLoggerProvider;
            provider.SetLogLevel("Steeltoe.Extensions.Logging.Test", LogLevel.Trace);
            Assert.True((logger).IsEnabled(LogLevel.Trace), "Trace level should have been enabled");
        }
    }
}
