﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.Console;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace Steeltoe.Extensions.Logging.Test
{
    public class DynamicLoggerProviderTest
    {
        private readonly Dictionary<string, string> defaultAppSettings = new Dictionary<string, string>()
        {
            ["Logging:IncludeScopes"] = "false",
            ["Logging:LogLevel:Default"] = "Information",
            ["Logging:LogLevel:System"] = "Information",
            ["Logging:LogLevel:Microsoft"] = "Information",
            ["Logging:LogLevel:A"] = "Information",
        };

        private readonly IConfigurationRoot defaultConfiguration;

        public DynamicLoggerProviderTest()
        {
            defaultConfiguration = new ConfigurationBuilder().AddInMemoryCollection(defaultAppSettings).Build();
        }

        [Fact]
        public void Create_CreatesCorrectLogger()
        {
            var provider = GetLoggerProvider(defaultConfiguration);

            var logger = provider.CreateLogger("A.B.C.D.TestClass");
            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(LogLevel.Information), "Information is not enabled when it should be");
            Assert.False(logger.IsEnabled(LogLevel.Debug), "Debug is enabled when it shouldn't be");
        }

        [Fact]
        public void SetLogLevel_UpdatesLogger()
        {
            var provider = GetLoggerProvider(defaultConfiguration) as DynamicConsoleLoggerProvider;

            var logger = provider.CreateLogger("A.B.C.D.TestClass");

            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(LogLevel.Critical));
            Assert.True(logger.IsEnabled(LogLevel.Error));
            Assert.True(logger.IsEnabled(LogLevel.Warning));
            Assert.True(logger.IsEnabled(LogLevel.Information));
            Assert.False(logger.IsEnabled(LogLevel.Debug));

            provider.SetLogLevel("A", LogLevel.Debug);
            Assert.True(logger.IsEnabled(LogLevel.Information));
            Assert.True(logger.IsEnabled(LogLevel.Debug));

            provider.SetLogLevel("A", LogLevel.Information);
            Assert.True(logger.IsEnabled(LogLevel.Information));
            Assert.False(logger.IsEnabled(LogLevel.Debug));
        }

        [Fact]
        public void SetLogLevel_UpdatesNamespaceDescendants()
        {
            // arrange (A* should log at Information)
            var provider = GetLoggerProvider(defaultConfiguration) as DynamicConsoleLoggerProvider;

            // act I: with original setup
            var childLogger = provider.CreateLogger("A.B.C");
            var configurations = provider.GetLoggerConfigurations();
            var tierOneNamespace = configurations.First(n => n.Name == "A");

            // assert I: base namespace is in the response, correctly
            Assert.Equal(LogLevel.Information, tierOneNamespace.EffectiveLevel);

            // act II: set A.B* to log at Trace
            provider.SetLogLevel("A.B", LogLevel.Trace);
            configurations = provider.GetLoggerConfigurations();
            tierOneNamespace = configurations.First(n => n.Name == "A");
            var tierTwoNamespace = configurations.First(n => n.Name == "A.B");

            // assert II: base hasn't changed but the one set at runtime and all descendants (including a concrete logger) have
            Assert.Equal(LogLevel.Information, tierOneNamespace.EffectiveLevel);
            Assert.Equal(LogLevel.Trace, tierTwoNamespace.EffectiveLevel);
            Assert.True(childLogger.IsEnabled(LogLevel.Trace));

            // act III: set A to something else, make sure it inherits down
            provider.SetLogLevel("A", LogLevel.Error);
            configurations = provider.GetLoggerConfigurations();
            tierOneNamespace = configurations.First(n => n.Name == "A");
            tierTwoNamespace = configurations.First(n => n.Name == "A.B");
            var grandchildLogger = provider.CreateLogger("A.B.C.D");

            // assert again
            Assert.Equal(LogLevel.Error, tierOneNamespace.EffectiveLevel);
            Assert.Equal(LogLevel.Error, tierTwoNamespace.EffectiveLevel);
            Assert.False(childLogger.IsEnabled(LogLevel.Warning));
            Assert.False(grandchildLogger.IsEnabled(LogLevel.Warning));
        }

        [Fact]
        public void SetLogLevel_Can_Reset_to_Default()
        {
            // arrange (A* should log at Information)
            var provider = GetLoggerProvider(defaultConfiguration) as DynamicConsoleLoggerProvider;

            // act I: with original setup
            var firstLogger = provider.CreateLogger("A.B.C");
            var configurations = provider.GetLoggerConfigurations();
            var tierOneNamespace = configurations.First(n => n.Name == "A");

            // assert I: base namespace is in the response, correctly
            Assert.Equal(LogLevel.Information, tierOneNamespace.EffectiveLevel);

            // act II: set A.B* to log at Trace
            provider.SetLogLevel("A.B", LogLevel.Trace);
            configurations = provider.GetLoggerConfigurations();
            tierOneNamespace = configurations.First(n => n.Name == "A");
            var tierTwoNamespace = configurations.First(n => n.Name == "A.B");

            // assert II: base hasn't changed but the one set at runtime and all descendants (including a concrete logger) have
            Assert.Equal(LogLevel.Information, tierOneNamespace.EffectiveLevel);
            Assert.Equal(LogLevel.Trace, tierTwoNamespace.EffectiveLevel);
            Assert.True(firstLogger.IsEnabled(LogLevel.Trace));

            // act III: reset A.B
            provider.SetLogLevel("A.B", null);
            configurations = provider.GetLoggerConfigurations();
            tierOneNamespace = configurations.First(n => n.Name == "A");
            tierTwoNamespace = configurations.First(n => n.Name == "A.B");
            var secondLogger = provider.CreateLogger("A.B.C.D");

            // assert again
            Assert.Equal(LogLevel.Information, tierOneNamespace.EffectiveLevel);
            Assert.Equal(LogLevel.Information, tierTwoNamespace.EffectiveLevel);
            Assert.True(firstLogger.IsEnabled(LogLevel.Information));
            Assert.True(secondLogger.IsEnabled(LogLevel.Information));
        }

        [Fact]
        public void GetLoggerConfigurations_ReturnsExpected()
        {
            var provider = GetLoggerProvider(defaultConfiguration) as DynamicConsoleLoggerProvider;
            _ = provider.CreateLogger("A.B.C.D.TestClass");

            var logConfig = provider.GetLoggerConfigurations();
            Assert.Equal(6, logConfig.Count);
            Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A.B.C.D.TestClass", null, LogLevel.Information), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A.B.C.D", null, LogLevel.Information), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A.B.C", null, LogLevel.Information), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A.B", null, LogLevel.Information), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A", LogLevel.Information, LogLevel.Information), logConfig);
        }

        [Fact]
        public void GetLoggerConfigurations_ReturnsExpected_After_SetLogLevel()
        {
            // arrange
            var provider = GetLoggerProvider(defaultConfiguration) as DynamicConsoleLoggerProvider;

            // act I
            _ = provider.CreateLogger("A.B.C.D.TestClass");
            var logConfig = provider.GetLoggerConfigurations();

            // assert I
            Assert.Equal(6, logConfig.Count);
            Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A.B.C.D.TestClass", null, LogLevel.Information), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A.B.C.D", null, LogLevel.Information), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A.B.C", null, LogLevel.Information), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A.B", null, LogLevel.Information), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A", LogLevel.Information, LogLevel.Information), logConfig);

            // act II
            provider.SetLogLevel("A.B", LogLevel.Trace);
            logConfig = provider.GetLoggerConfigurations();

            // assert II
            Assert.Equal(6, logConfig.Count);
            Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A.B.C.D.TestClass", null, LogLevel.Trace), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A.B.C.D", null, LogLevel.Trace), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A.B.C", null, LogLevel.Trace), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A.B", null, LogLevel.Trace), logConfig);
            Assert.Contains(new DynamicLoggerConfiguration("A", LogLevel.Information, LogLevel.Information), logConfig);
        }

        [Fact]
        public void SetLogLevel_Works_OnDefault()
        {
            // arrange
            var provider = GetLoggerProvider(defaultConfiguration) as DynamicConsoleLoggerProvider;
            var originalLogConfig = provider.GetLoggerConfigurations();

            // act
            provider.SetLogLevel("Default", LogLevel.Trace);
            var updatedLogConfig = provider.GetLoggerConfigurations();

            // assert
            Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), originalLogConfig);
            Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Trace), updatedLogConfig);
        }

        [Fact]
        public void ResetLogLevel_Works_OnDefault()
        {
            // arrange
            var provider = GetLoggerProvider(defaultConfiguration) as DynamicConsoleLoggerProvider;
            var originalLogConfig = provider.GetLoggerConfigurations();

            // act
            provider.SetLogLevel("Default", LogLevel.Trace);
            var updatedLogConfig = provider.GetLoggerConfigurations();
            provider.SetLogLevel("Default", null);
            var resetConfig = provider.GetLoggerConfigurations();

            // assert
            Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), originalLogConfig);
            Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Trace), updatedLogConfig);
            Assert.Contains(new DynamicLoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), resetConfig);
        }

        [Fact(Skip = "This method of console redirection doesn't work in Logging 3.0")]
        public void LoggerLogs_At_Configured_Setting()
        {
            // arrange
            var provider = GetLoggerProvider(defaultConfiguration) as DynamicConsoleLoggerProvider;
            var logger = provider.CreateLogger("A.B.C.D.TestClass");

            // act I - log at all levels, expect Info and above to work
            using (var unConsole = new ConsoleOutputBorrower())
            {
                WriteLogEntries(logger);

                // pause the thread to allow the logging to happen
                Thread.Sleep(100);

                var logged = unConsole.ToString();

                // assert I
                Assert.Contains("Critical message", logged);
                Assert.Contains("Error message", logged);
                Assert.Contains("Warning message", logged);
                Assert.Contains("Informational message", logged);
                Assert.DoesNotContain("Debug message", logged);
                Assert.DoesNotContain("Trace message", logged);
            }

            // act II - adjust rules, expect Error and above to work
            provider.SetLogLevel("A.B.C.D", LogLevel.Error);
            using (var unConsole = new ConsoleOutputBorrower())
            {
                WriteLogEntries(logger);

                // pause the thread to allow the logging to happen
                Thread.Sleep(100);

                var logged2 = unConsole.ToString();

                // assert II
                Assert.Contains("Critical message", logged2);
                Assert.Contains("Error message", logged2);
                Assert.DoesNotContain("Warning message", logged2);
                Assert.DoesNotContain("Informational message", logged2);
                Assert.DoesNotContain("Debug message", logged2);
                Assert.DoesNotContain("Trace message", logged2);
            }

            // act III - adjust rules, expect Trace and above to work
            provider.SetLogLevel("A", LogLevel.Trace);
            using (var unConsole = new ConsoleOutputBorrower())
            {
                WriteLogEntries(logger);

                // pause the thread to allow the logging to happen
                Thread.Sleep(100);

                var logged3 = unConsole.ToString();

                // assert III
                Assert.Contains("Critical message", logged3);
                Assert.Contains("Error message", logged3);
                Assert.Contains("Warning message", logged3);
                Assert.Contains("Informational message", logged3);
                Assert.Contains("Debug message", logged3);
                Assert.Contains("Trace message", logged3);
            }

            // act IV - adjust rules, expect nothing to work
            provider.SetLogLevel("A", LogLevel.None);
            using (var unConsole = new ConsoleOutputBorrower())
            {
                WriteLogEntries(logger);

                // pause the thread to allow the logging to happen
                Thread.Sleep(100);

                var logged4 = unConsole.ToString();

                // assert IV
                Assert.DoesNotContain("Critical message", logged4);
                Assert.DoesNotContain("Error message", logged4);
                Assert.DoesNotContain("Warning message", logged4);
                Assert.DoesNotContain("Informational message", logged4);
                Assert.DoesNotContain("Debug message", logged4);
                Assert.DoesNotContain("Trace message", logged4);
            }

            // act V - reset the rules, expect Info and above to work
            provider.SetLogLevel("A", null);
            using (var unConsole = new ConsoleOutputBorrower())
            {
                WriteLogEntries(logger);

                // pause the thread to allow the logging to happen
                Thread.Sleep(100);

                var logged5 = unConsole.ToString();

                // assert V
                Assert.NotNull(provider.GetLoggerConfigurations().First(c => c.Name == "A"));
                Assert.Contains("Critical message", logged5);
                Assert.Contains("Error message", logged5);
                Assert.Contains("Warning message", logged5);
                Assert.Contains("Informational message", logged5);
                Assert.DoesNotContain("Debug message", logged5);
                Assert.DoesNotContain("Trace message", logged5);
            }
        }

        private void WriteLogEntries(ILogger logger)
        {
            logger.LogCritical("Critical message");
            logger.LogError("Error message");
            logger.LogWarning("Warning message");
            logger.LogInformation("Informational message");
            logger.LogDebug("Debug message");
            logger.LogTrace("Trace message");
        }

        private ILoggerProvider GetLoggerProvider(IConfiguration config)
        {
            var serviceProvider = new ServiceCollection()
                        .AddLogging(builder => builder
                            .AddConfiguration(config.GetSection("Logging"))
                            .AddDynamicConsole()
                            .AddFilter<DynamicConsoleLoggerProvider>(null, LogLevel.Trace))
                        .BuildServiceProvider();

            var loggerProviderConfiguration = serviceProvider.GetService<ILoggerProviderConfiguration<ConsoleLoggerProvider>>();
            return serviceProvider.GetRequiredService<ILoggerProvider>();
        }
    }
}