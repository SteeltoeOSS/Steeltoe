// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.Loggers;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Loggers;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public async Task AddLoggersActuator_AddsCorrectServices()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddLoggersActuator();
        services.AddSingleton(configuration);
        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        var handler = serviceProvider.GetService<ILoggersEndpointHandler>();
        handler.Should().NotBeNull();
    }
}
