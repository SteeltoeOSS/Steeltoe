// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Serilog.Events;
using Serilog.Exceptions;
using System.Threading;
using Xunit;

namespace Steeltoe.Extensions.Logging.SerilogDynamicLogger.Test
{
    public class SerilogDynamicWebhostBuilderTest
    {
        public SerilogDynamicWebhostBuilderTest()
        {
            SerilogDynamicProvider.ClearLogger();
        }

        [Fact]
        public void OnlyApplicableFilters_AreApplied()
        {
            // arrange
            var testSink = new TestSink();

            // act
            var host = new WebHostBuilder()
                .UseStartup<Startup>()
                .UseSerilogDynamicConsole((context, loggerConfiguration) =>
                {
                    loggerConfiguration
                    .MinimumLevel.Error()
                    .Enrich.WithExceptionDetails()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .WriteTo.Sink(testSink);
                })
                .Build();

            // assert
            var logs = testSink.GetLogs();
            Assert.NotEmpty(logs);
            Assert.DoesNotContain("info", logs);
            Assert.Contains("error", logs);
        }
    }
}
