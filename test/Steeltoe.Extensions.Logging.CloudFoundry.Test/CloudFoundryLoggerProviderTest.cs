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
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Extensions.Logging.CloudFoundry.Test
{
    public class CloudFoundryLoggerProviderTest
    {
        [Fact]
        public void Create_CreatesCorrectLogger()
        {
            var provider = new CloudFoundryLoggerProvider(GetLoggerSettings());
            LoggerFactory fac = new LoggerFactory();
            fac.AddProvider(provider);

            ILogger logger = fac.CreateLogger(typeof(A.B.C.D.TestClass));
            Assert.NotNull(logger);
            Assert.True(logger.IsEnabled(LogLevel.Information));
            Assert.False(logger.IsEnabled(LogLevel.Debug));

            provider.Dispose();
        }

        [Fact]
        public void SetLogLevel_UpdatesLogger()
        {
            var provider = new CloudFoundryLoggerProvider(GetLoggerSettings());
            LoggerFactory fac = new LoggerFactory();
            fac.AddProvider(provider);

            ILogger logger = fac.CreateLogger(typeof(A.B.C.D.TestClass));
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

            provider.Dispose();
        }

        [Fact]
        public void SetLogLevel_UpdatesNamespaceDescendants()
        {
            // arrange (A* should log at Information)
            var provider = new CloudFoundryLoggerProvider(GetLoggerSettings());

            // act I: with original setup
            var configurations = provider.GetLoggerConfigurations();
            var tierOneNamespace = configurations.First(n => n.Name == "A");

            // assert I: base namespace is in the response, correctly
            Assert.Equal(LogLevel.Information, tierOneNamespace.EffectiveLevel);

            // act II: set A.B* to log at Trace
            provider.SetLogLevel("A.B", LogLevel.Trace);
            configurations = provider.GetLoggerConfigurations();
            var childLogger = provider.CreateLogger("A.B.C");
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

            provider.Dispose();
        }

        [Fact]
        public void GetLoggerConfigurations_ReturnsExpected()
        {
            var provider = new CloudFoundryLoggerProvider(GetLoggerSettings());
            LoggerFactory fac = new LoggerFactory();
            fac.AddProvider(provider);

            ILogger logger = fac.CreateLogger(typeof(A.B.C.D.TestClass));

            var logConfig = provider.GetLoggerConfigurations();
            Assert.Equal(8, logConfig.Count);
            Assert.Contains(new LoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), logConfig);
            Assert.Contains(new LoggerConfiguration("A.B.C.D.TestClass", null, LogLevel.Information), logConfig);
            Assert.Contains(new LoggerConfiguration("A.B.C.D", null, LogLevel.Information), logConfig);
            Assert.Contains(new LoggerConfiguration("A.B.C", null, LogLevel.Information), logConfig);
            Assert.Contains(new LoggerConfiguration("A.B", null, LogLevel.Information), logConfig);
            Assert.Contains(new LoggerConfiguration("A", LogLevel.Information, LogLevel.Information), logConfig);

            provider.Dispose();
        }

        [Fact]
        public void GetLoggerConfigurations_ReturnsExpected_After_SetLogLevel()
        {
            // arrange
            var provider = new CloudFoundryLoggerProvider(GetLoggerSettings());
            LoggerFactory fac = new LoggerFactory();
            fac.AddProvider(provider);

            // act I
            ILogger logger = fac.CreateLogger(typeof(A.B.C.D.TestClass));
            var logConfig = provider.GetLoggerConfigurations();

            // assert I
            Assert.Equal(8, logConfig.Count);
            Assert.Contains(new LoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), logConfig);
            Assert.Contains(new LoggerConfiguration("A.B.C.D.TestClass", null, LogLevel.Information), logConfig);
            Assert.Contains(new LoggerConfiguration("A.B.C.D", null, LogLevel.Information), logConfig);
            Assert.Contains(new LoggerConfiguration("A.B.C", null, LogLevel.Information), logConfig);
            Assert.Contains(new LoggerConfiguration("A.B", null, LogLevel.Information), logConfig);
            Assert.Contains(new LoggerConfiguration("A", LogLevel.Information, LogLevel.Information), logConfig);

            // act II
            provider.SetLogLevel("A.B", LogLevel.Trace);
            logConfig = provider.GetLoggerConfigurations();

            // assert II
            Assert.Equal(8, logConfig.Count);
            Assert.Contains(new LoggerConfiguration("Default", LogLevel.Information, LogLevel.Information), logConfig);
            Assert.Contains(new LoggerConfiguration("A.B.C.D.TestClass", null, LogLevel.Trace), logConfig);
            Assert.Contains(new LoggerConfiguration("A.B.C.D", null, LogLevel.Trace), logConfig);
            Assert.Contains(new LoggerConfiguration("A.B.C", null, LogLevel.Trace), logConfig);
            Assert.Contains(new LoggerConfiguration("A.B", null, LogLevel.Trace), logConfig);
            Assert.Contains(new LoggerConfiguration("A", LogLevel.Information, LogLevel.Information), logConfig);

            provider.Dispose();
        }

        private ConsoleLoggerSettings GetLoggerSettings()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["Logging:IncludeScopes"] = "false",
                ["Logging:LogLevel:Default"] = "Information",
                ["Logging:LogLevel:System"] = "Information",
                ["Logging:LogLevel:Microsoft"] = "Information",
                ["Logging:LogLevel:A"] = "Information",
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();

            return new ConsoleLoggerSettings().FromConfiguration(config);
        }
    }
}