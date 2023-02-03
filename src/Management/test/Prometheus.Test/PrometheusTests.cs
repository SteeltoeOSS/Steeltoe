// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace Steeltoe.Management.Prometheus.Test;
public class PrometheusTests
{

    [Fact]
    public void AddPrometheusActuatorServices_ThrowsOnNulls()
    {
        //const IServiceCollection services = null;
        //IServiceCollection services2 = new ServiceCollection();
        //const IConfigurationRoot configuration = null;

        //var ex = Assert.Throws<ArgumentNullException>(() => services.AddPrometheusActuatorServices(configuration));
        //Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);

        //var ex2 = Assert.Throws<ArgumentNullException>(() => services2.AddPrometheusActuatorServices(configuration));
        //Assert.Contains(nameof(configuration), ex2.Message, StringComparison.Ordinal);
    }
}
