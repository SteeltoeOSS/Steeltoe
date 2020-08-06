// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Serilog.Events;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Extensions.Logging.DynamicSerilog.Test
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
                { "Serilog:MinimumLevel:Override:Steeltoe", "Information" },
                { "Serilog:SubloggerConfigKeyExclusions:0", "Enrichers" },
                { "Serilog:SubloggerConfigKeyExclusions:1", "WriteTo" },
                { "Serilog:SubloggerConfigKeyExclusions:2", "MinimumLevel" },
            };
            var builder = new ConfigurationBuilder().AddInMemoryCollection(appSettings);

            // act
            var serilogOptions = new SerilogOptions(builder.Build());

            // assert
            Assert.Equal(LogEventLevel.Error, serilogOptions.MinimumLevel.Default);
            Assert.Equal(LogEventLevel.Warning, serilogOptions.MinimumLevel.Override["Microsoft"]);
            Assert.Equal(LogEventLevel.Information, serilogOptions.MinimumLevel.Override["Steeltoe"]);
            Assert.Equal(LogEventLevel.Verbose, serilogOptions.MinimumLevel.Override["Steeltoe.Extensions"]);
            Assert.Collection<string>(
                serilogOptions.SubloggerConfigKeyExclusions,
                x => Assert.Contains("Enrichers", x),
                x => Assert.Contains("WriteTo", x),
                x => Assert.Contains("MinimumLevel", x));
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
            Assert.Collection<string>(
              serilogOptions.SubloggerConfigKeyExclusions,
              x => Assert.Contains("WriteTo", x),
              x => Assert.Contains("MinimumLevel", x));
        }
    }
}
