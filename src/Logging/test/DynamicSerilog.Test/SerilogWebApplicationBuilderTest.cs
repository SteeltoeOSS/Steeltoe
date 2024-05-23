// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Logging.DynamicSerilog.Test;

public sealed class SerilogWebApplicationBuilderTest
{
    public SerilogWebApplicationBuilderTest()
    {
        DynamicSerilogLoggerProvider.ClearLogger();
    }

    [Fact]
    public void OnlyApplicableFilters_AreApplied()
    {
        var testSink = new TestSink();

        WebApplication host = TestHelpers.GetTestWebApplicationBuilder().AddDynamicSerilog((_, loggerConfiguration) =>
            loggerConfiguration.MinimumLevel.Error().Enrich.WithExceptionDetails().MinimumLevel.Override("Microsoft", LogEventLevel.Warning).WriteTo
                .Sink(testSink)).Build();

        var startup = new Startup(host.Services.GetRequiredService<ILogger<Startup>>());
        startup.ConfigureServices(null);

        List<string> logs = testSink.GetLogs();

        logs.Should().NotBeEmpty();
        logs.Should().Contain("error");
        logs.Should().NotContain("info");
    }

    [Fact]
    public void OnlyApplicableFilters_AreApplied_via_Options()
    {
        var appSettings = new Dictionary<string, string?>
        {
            { "Serilog:Using:0", "Steeltoe.Logging.DynamicSerilog.Test" },
            { "Serilog:MinimumLevel:Default", "Error" },
            { "Serilog:MinimumLevel:Override:Microsoft", "Warning" },
            { "Serilog:WriteTo:Name", "TestSink" }
        };

        WebApplicationBuilder hostBuilder = TestHelpers.GetTestWebApplicationBuilder();
        hostBuilder.Configuration.AddInMemoryCollection(appSettings);
        hostBuilder.AddDynamicSerilog();
        WebApplication host = hostBuilder.Build();

        object logger = host.Services.GetRequiredService<ILogger<SerilogWebApplicationBuilderTest>>();
        var startup = new Startup(host.Services.GetRequiredService<ILogger<Startup>>());
        startup.ConfigureServices(null);

        ILogEventSink[] sinks = SerilogWebHostBuilderTest.GetSinks(logger);
        sinks.Should().NotBeEmpty();

        TestSink? testSink = sinks.OfType<TestSink>().FirstOrDefault();
        testSink.Should().NotBeNull();

        List<string> logs = testSink!.GetLogs();

        logs.Should().NotBeEmpty();
        logs.Should().Contain("error");
        logs.Should().NotContain("info");
    }
}
