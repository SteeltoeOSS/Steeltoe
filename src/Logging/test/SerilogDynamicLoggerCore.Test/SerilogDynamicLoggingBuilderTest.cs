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
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Logging.SerilogDynamicLogger.Test
{
    public class SerilogDynamicLoggingBuilderTest
    {
        private static Dictionary<string, string> appsettings = new Dictionary<string, string>()
        {
            { "Serilog:MinimumLevel:Default", "Verbose" }, // Sets level of root logger so has to be higher than any sub logger
            { "Serilog:MinimumLevel:Override:Microsoft", "Warning" },
            { "Serilog:MinimumLevel:Override:Steeltoe.Extensions", "Verbose" },
            { "Serilog:MinimumLevel:Override:Steeltoe", "Information" },
            { "Serilog:MinimumLevel:Override:A", "Information" },
            { "Serilog:MinimumLevel:Override:A.B.C.D", "Fatal" },
            { "Serilog:WriteTo:Name", "Console" },
        };

        [Fact]
        public void OnlyApplicableFilters_AreApplied()
        {
            // arrange
            var appsettings = new Dictionary<string, string>()
            {
                ["Logging:IncludeScopes"] = "false",
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:foo:LogLevel:A.B.C.D.TestClass"] = "None"
            };
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(builder =>
                {
                    builder.AddSerilogDynamicConsole();
                })
                .BuildServiceProvider();

            // act
            var logger = services.GetService(typeof(ILogger<A.B.C.D.TestClass>)) as ILogger<A.B.C.D.TestClass>;

            // assert
            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(LogLevel.Information), "Information level should be enabled");
            Assert.False(logger.IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
        }

        [Fact]
        public void DynamicLevelSetting_WorksWith_ConsoleFilters()
        {
            // arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(builder =>
                {
                    builder.AddSerilogDynamicConsole();
                })
                .BuildServiceProvider();

            // act
            var logger = services.GetService(typeof(ILogger<A.B.C.D.TestClass>)) as ILogger<A.B.C.D.TestClass>;

            // assert
            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(LogLevel.Critical), "Critical level should be enabled");
            Assert.False(logger.IsEnabled(LogLevel.Error), "Error level should NOT be enabled");
            Assert.False(logger.IsEnabled(LogLevel.Warning), "Warning level should NOT be enabled");
            Assert.False(logger.IsEnabled(LogLevel.Debug), "Debug level should NOT be enabled");
            Assert.False(logger.IsEnabled(LogLevel.Trace), "Trace level should NOT be enabled yet");

            // change the log level and confirm it worked
            var provider = services.GetRequiredService(typeof(ILoggerProvider)) as SerilogDynamicProvider;
            provider.SetLogLevel("A.B.C.D", LogLevel.Trace);
            var levels = provider.GetLoggerConfigurations().Where(c => c.Name.StartsWith("A.B.C.D"))
                .Select(x => x.EffectiveLevel);

            Assert.NotNull(levels);
            Assert.True(levels.All(x => x == LogLevel.Trace));
        }

        [Fact]
        public void AddDynamicConsole_AddsAllLoggerProviders()
        {
            // arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(builder =>
                {
                    builder.AddSerilogDynamicConsole();
                }).BuildServiceProvider();

            // act
            var dlogProvider = services.GetService<IDynamicLoggerProvider>();
            var logProviders = services.GetServices<ILoggerProvider>();

            // assert
            Assert.NotNull(dlogProvider);
            Assert.NotEmpty(logProviders);
            Assert.Single(logProviders);
            Assert.IsType<SerilogDynamicProvider>(logProviders.SingleOrDefault());
        }

        [Fact]
        public void AddDynamicConsole_AddsLoggerProvider_DisposeTwiceSucceeds()
        {
            // arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(builder =>
                {
                    builder.AddSerilogDynamicConsole();
                }).BuildServiceProvider();

            // act
            var dlogProvider = services.GetService<IDynamicLoggerProvider>();
            var logProviders = services.GetServices<ILoggerProvider>();

            // assert
            services.Dispose();
            dlogProvider.Dispose();
        }

        [Fact]
        public void AddDynamicConsole_WithConfigurationParam_AddsServices()
        {
            // arrange
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(builder => builder.AddSerilogDynamicConsole())
                .BuildServiceProvider();

            // act
            var dlogProvider = services.GetService<IDynamicLoggerProvider>();
            var logProviders = services.GetServices<ILoggerProvider>();

            // assert
            Assert.NotNull(dlogProvider);
            Assert.NotEmpty(logProviders);
            Assert.Single(logProviders);
            Assert.IsType<SerilogDynamicProvider>(logProviders.SingleOrDefault());
        }
    }
}
