// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Trace;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Trace;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddTraceActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string?>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:trace:enabled"] = "false"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        using var listener = new DiagnosticListener("Test");

        services.AddLogging();
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddSingleton(listener);

        services.AddTraceActuator();

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<TraceEndpointOptions>>();
        Assert.NotNull(options);
        var handler = serviceProvider.GetService<IHttpTraceEndpointHandler>();
        Assert.NotNull(handler);
    }
}
