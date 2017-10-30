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
using Xunit;

namespace Steeltoe.Extensions.Logging.CloudFoundry.Test
{
    public class CloudFoundryConfigurationExtensionsTest
    {
        [Fact]
        public void FromConfiguration_Parses_Correctly()
        {
            // arrange
            var appSettings = new Dictionary<string, string>
            {
                { "Logging:IncludeScopes", "true" },
                { "Logging:LogLevel:Default", "Error" },
                { "Logging:LogLevel:Microsoft", "Warning" },
                { "Logging:LogLevel:Steeltoe", "Information" },
            };
            var builder = new ConfigurationBuilder().AddInMemoryCollection(appSettings);
            var settings = new ConsoleLoggerSettings();

            // act
            settings.FromConfiguration(builder.Build());

            // assert
            Assert.True(settings.IncludeScopes);
            Assert.Equal(LogLevel.Error, settings.Switches["Default"]);
            Assert.Equal(LogLevel.Warning, settings.Switches["Microsoft"]);
            Assert.Equal(LogLevel.Information, settings.Switches["Steeltoe"]);
        }
        [Fact]
        public void FromConfiguration_NoError_When_NotConfigured()
        {
            // arrange
            var appSettings = new Dictionary<string, string>();
            var builder = new ConfigurationBuilder().AddInMemoryCollection(appSettings);
            var settings = new ConsoleLoggerSettings();

            // act
            settings.FromConfiguration(builder.Build());

            // assert
            Assert.False(settings.IncludeScopes);
            Assert.Equal(LogLevel.None, settings.Switches["Default"]);
        }
    }
}
