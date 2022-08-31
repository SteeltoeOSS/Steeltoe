// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Steeltoe.Management.Endpoint.Trace.Test;

public class ServiceCollectionTests
{
    [Fact]
    public void AddTraceActuatorServices_ThrowsOnNulls()
    {
        const IServiceCollection services = null;
        IServiceCollection services2 = new ServiceCollection();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddTraceActuatorServices(configuration, MediaTypeVersion.V2));
        Assert.Contains(nameof(services), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services2.AddTraceActuatorServices(configuration, MediaTypeVersion.V2));
        Assert.Contains(nameof(configuration), ex2.Message);
    }
}
