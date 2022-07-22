// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Mappings;

public class ServiceCollectionTests
{
    [Fact]
    public void AddMappingsActuatorServices_ThrowsOnNulls()
    {
        IServiceCollection services = null;
        IServiceCollection services2 = new ServiceCollection();
        IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddMappingsActuatorServices(services, config));
        Assert.Contains(nameof(services), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddMappingsActuatorServices(services2, config));
        Assert.Contains(nameof(config), ex2.Message);
    }
}