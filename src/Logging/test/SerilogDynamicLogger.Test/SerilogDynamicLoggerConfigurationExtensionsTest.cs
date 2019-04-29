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
using Serilog.Events;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Extensions.Logging.SerilogDynamicLogger.Test
{
    public class SerilogDynamicLoggerConfigurationExtensionsTest
    {
        [Fact]
        public void SerilogOptions_Set_Correctly()
        {
            // arrange
            var appSettings = new Dictionary<string, string>
            {
                { "Serilog:MinimumLevel:Default", "Error" },
                { "Serilog:MinimumLevel:Override:Microsoft", "Warning" },
                { "Serilog:MinimumLevel:Override:Steeltoe.Extensions", "Verbose" },
                { "Serilog:MinimumLevel:Override:Steeltoe", "Information" }
            };
            var builder = new ConfigurationBuilder().AddInMemoryCollection(appSettings);

            // act
            var serilogOptions = new SerilogOptions(builder.Build());

            // assert
            Assert.Equal(LogEventLevel.Error, serilogOptions.MinimumLevel.Default);
            Assert.Equal(LogEventLevel.Warning, serilogOptions.MinimumLevel.Override["Microsoft"]);
            Assert.Equal(LogEventLevel.Information, serilogOptions.MinimumLevel.Override["Steeltoe"]);
            Assert.Equal(LogEventLevel.Verbose, serilogOptions.MinimumLevel.Override["Steeltoe.Extensions"]);
        }

        [Fact]
        public void SerilogOptions_NoError_When_NotConfigured()
        {
            // arrange
            var appSettings = new Dictionary<string, string>();
            var builder = new ConfigurationBuilder().AddInMemoryCollection(appSettings);

            // act
            var serilogOptions = new SerilogOptions(builder.Build());

            // assert
            Assert.Equal(LogEventLevel.Verbose, serilogOptions.MinimumLevel.Default);
        }
    }
}
