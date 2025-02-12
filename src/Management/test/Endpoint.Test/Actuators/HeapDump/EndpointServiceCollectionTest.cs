// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HeapDump;

public sealed class EndpointServiceCollectionTest : BaseTest
{
    [Fact]
    public async Task AddHeapDumpActuator_AddsCorrectServices()
    {
        IConfiguration configuration = new ConfigurationBuilder().Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddHeapDumpActuator();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(true);

        serviceProvider.GetService<IHeapDumpEndpointHandler>().Should().BeOfType<HeapDumpEndpointHandler>();
        serviceProvider.GetService<IHeapDumper>().Should().BeOfType<HeapDumper>();
    }
}
