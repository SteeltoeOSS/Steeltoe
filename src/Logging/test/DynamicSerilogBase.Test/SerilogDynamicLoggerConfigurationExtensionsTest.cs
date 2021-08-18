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
            };
            var builder = new ConfigurationBuilder().AddInMemoryCollection(appSettings);

            // act
            var serilogOptions = new SerilogOptions();
            serilogOptions.SetSerilogOptions(builder.Build());

            // assert
            Assert.Equal(LogEventLevel.Error, serilogOptions.MinimumLevel.Default);
            Assert.Equal(LogEventLevel.Warning, serilogOptions.MinimumLevel.Override["Microsoft"]);
            Assert.Equal(LogEventLevel.Information, serilogOptions.MinimumLevel.Override["Steeltoe"]);
            Assert.Equal(LogEventLevel.Verbose, serilogOptions.MinimumLevel.Override["Steeltoe.Extensions"]);
            Assert.NotNull(serilogOptions.GetSerilogConfiguration());
        }

        [Fact]
        public void SerilogOptions_Set_Correctly_When_MinimumLevel_Is_String()
        {
            // arrange
            var appSettings = new Dictionary<string, string>
            {
                { "Serilog:MinimumLevel", "Error" }
            };
            var builder = new ConfigurationBuilder().AddInMemoryCollection(appSettings);

            // act
            var serilogOptions = new SerilogOptions();
            serilogOptions.SetSerilogOptions(builder.Build());

            // assert
            Assert.Equal(LogEventLevel.Error, serilogOptions.MinimumLevel.Default);
            Assert.NotNull(serilogOptions.GetSerilogConfiguration());
        }

        [Fact]
        public void SerilogOptions_Set_Correctly_Via_LoggerConfiguration()
        {
            // arrange
            var loggerConfiguration = new Serilog.LoggerConfiguration()
                                                 .MinimumLevel.Debug()
                                                 .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                                                 .MinimumLevel.Override("Steeltoe.Extensions", LogEventLevel.Verbose)
                                                 .MinimumLevel.Override("Steeltoe", LogEventLevel.Information);

            // act
            var serilogOptions = new SerilogOptions();
            serilogOptions.SetSerilogOptions(loggerConfiguration);

            // assert
            Assert.Equal(LogEventLevel.Debug, serilogOptions.MinimumLevel.Default);
            Assert.Equal(LogEventLevel.Warning, serilogOptions.MinimumLevel.Override["Microsoft"]);
            Assert.Equal(LogEventLevel.Verbose, serilogOptions.MinimumLevel.Override["Steeltoe.Extensions"]);
            Assert.NotNull(serilogOptions.GetSerilogConfiguration());
        }

        [Fact]
        public void SerilogOptions_NoError_When_NotConfigured()
        {
            // arrange
            var appSettings = new Dictionary<string, string>();
            var builder = new ConfigurationBuilder().AddInMemoryCollection(appSettings);

            // act
            var serilogOptions = new SerilogOptions();
            serilogOptions.SetSerilogOptions(builder.Build());

            // assert
            Assert.Equal(LogEventLevel.Information, serilogOptions.MinimumLevel.Default);
            Assert.NotNull(serilogOptions.GetSerilogConfiguration());
        }
    }
}
