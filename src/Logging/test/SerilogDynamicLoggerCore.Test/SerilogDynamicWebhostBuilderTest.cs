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

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Events;
using Serilog.Exceptions;
using Xunit;

namespace Steeltoe.Extensions.Logging.SerilogDynamicLogger.Test
{
    public class SerilogDynamicWebhostBuilderTest
    {
        [Fact]
        public void OnlyApplicableFilters_AreApplied()
        {
            // arrange
            var testSink = new TestSink();

            // act
#if NETCOREAPP3_0
            var host = new HostBuilder()
            .UseSerilogDynamicConsole((context, loggerConfiguration) =>
            {
                loggerConfiguration
                    .MinimumLevel.Error()
                    .Enrich.WithExceptionDetails()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .WriteTo.Sink(testSink, LogEventLevel.Error);
            })
            .ConfigureServices((context, services) =>
            {
                var logger = services.BuildServiceProvider().GetRequiredService<ILogger<Startup>>();
                logger.LogError("error");
                logger.LogInformation("info");
            })
            .Build();
#else
            var host = new WebHostBuilder()
            .UseStartup<Startup>()
            .UseSerilogDynamicConsole((context, loggerConfiguration) =>
            {
                loggerConfiguration
                .MinimumLevel.Error()
                .Enrich.WithExceptionDetails()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.Sink(testSink, LogEventLevel.Error);
            })
            .Build();
#endif

            // assert
            var logs = testSink.GetLogs();
            Assert.NotEmpty(logs);
            Assert.Contains("error", logs);
            Assert.DoesNotContain("info", logs);
        }
    }
}
