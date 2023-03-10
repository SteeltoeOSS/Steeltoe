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

public class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddTraceActuator_ThrowsOnNulls()
    {
        const IServiceCollection services = null;
        IServiceCollection services2 = new ServiceCollection();

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddTraceActuator());
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddTraceActuator_AddsCorrectServices()
    {
        var services = new ServiceCollection();

        var appSettings = new Dictionary<string, string>
        {
            ["management:endpoints:enabled"] = "false",
            ["management:endpoints:path"] = "/cloudfoundryapplication",
            ["management:endpoints:trace:enabled"] = "false"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appSettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        var listener = new DiagnosticListener("Test");
        services.AddSingleton<IConfiguration>(configurationRoot);
        services.AddSingleton(listener);

        services.AddTraceActuator(configurationRoot);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<IOptionsMonitor<TraceEndpointOptions>>();
        Assert.NotNull(options);
        var ep = serviceProvider.GetService<IHttpTraceEndpoint>();
        Assert.NotNull(ep);
        listener.Dispose();
    }
}
