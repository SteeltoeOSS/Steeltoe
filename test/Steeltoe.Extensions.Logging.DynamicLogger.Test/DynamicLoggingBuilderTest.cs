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
            ["Logging:LogLevel:Steeltoe.Extensions.Logging.Test"] = "Information",
            ["Logging:LogLevel:Default"] = "Warning"
        };

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
    }
}
