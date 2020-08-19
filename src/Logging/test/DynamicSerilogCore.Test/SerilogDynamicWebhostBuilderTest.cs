// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Serilog.Events;
using Serilog.Exceptions;
using Xunit;

namespace Steeltoe.Extensions.Logging.DynamicSerilog.Test
{
    public class SerilogDynamicWebhostBuilderTest
    {
        [Fact]
        public void OnlyApplicableFilters_AreApplied()
        {
            // arrange
            var testSink = new TestSink();

            // act
            var host = new WebHostBuilder()
                .UseStartup<Startup>()
                .AddDynamicSerilog((context, loggerConfiguration) =>
                {
                    loggerConfiguration
                        .MinimumLevel.Error()
                        .Enrich.WithExceptionDetails()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .WriteTo.Sink(testSink, LogEventLevel.Error);
                })
                .Build();

            // assert
            var logs = testSink.GetLogs();
            Assert.NotEmpty(logs);
            Assert.Contains("error", logs);
            Assert.DoesNotContain("info", logs);
        }
    }
}
