// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Steeltoe.Management.Prometheus.Test;

public sealed class PrometheusTests
{
    [Fact]
    public void AddPrometheusActuator_SetsUpRequiredServices()
    {
        var services = new ServiceCollection();
        services.AddPrometheusActuator();

        using ServiceProvider provider = services.BuildServiceProvider();
        var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PrometheusEndpointOptions>>();

        optionsMonitor.CurrentValue.Path.Should().Be("prometheus");
    }
}
