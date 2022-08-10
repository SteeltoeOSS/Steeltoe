// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Test;
using Xunit;

namespace Steeltoe.Management.Endpoint.Trace.Test;

public class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public void AddTraceActuator_ThrowsOnNulls()
    {
        const IServiceCollection services = null;
        IServiceCollection services2 = new ServiceCollection();
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddTraceActuator());
        Assert.Contains(nameof(services), ex.Message);
        var ex2 = Assert.Throws<ArgumentNullException>(() => services2.AddTraceActuator());
        Assert.Contains(nameof(config), ex2.Message);
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
        IConfigurationRoot config = configurationBuilder.Build();
        var listener = new DiagnosticListener("Test");
        services.AddSingleton(listener);

        services.AddTraceActuator(config);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var options = serviceProvider.GetService<ITraceOptions>();
        Assert.NotNull(options);
        var ep = serviceProvider.GetService<HttpTraceEndpoint>();
        Assert.NotNull(ep);
        listener.Dispose();
    }
}
