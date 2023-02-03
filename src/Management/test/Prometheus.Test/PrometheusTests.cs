// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Steeltoe.Management.Prometheus.Test;
public class PrometheusTests
{

    [Fact]
    public void AddPrometheusActuator_ThrowsOnNulls()
    {
        const IServiceCollection services = null;
        IServiceCollection services2 = new ServiceCollection();

        var ex = Assert.Throws<ArgumentNullException>(services.AddPrometheusActuator);
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPrometheusActuator_SetupsRequiredServices()
    {
        var services = new ServiceCollection();
        services.AddPrometheusActuator();
        var provider = services.BuildServiceProvider();
        var options = provider.GetService<IOptionsMonitor<PrometheusEndpointOptions>>();
        Assert.NotNull(options);
    }
}
