// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Logging.DynamicSerilog.Test
{
    public class SerilogDynamicLoggingBuilderTest
    {
        private static readonly Dictionary<string, string> Appsettings = new Dictionary<string, string>()
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
                    builder.AddDynamicSerilog();
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
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(builder =>
                {
                    builder.AddDynamicSerilog();
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
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(builder =>
                {
                    builder.AddDynamicSerilog();
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
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(builder =>
                {
                    builder.AddDynamicSerilog();
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
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(Appsettings).Build();
            var services = new ServiceCollection()
                .AddSingleton<IConfiguration>(configuration)
                .AddLogging(builder => builder.AddDynamicSerilog())
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
