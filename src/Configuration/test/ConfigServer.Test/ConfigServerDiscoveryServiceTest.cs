// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.TestResources;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerDiscoveryServiceTest
{
    [Fact]
    public async Task ConfigServerDiscoveryService_FindsNoDiscoveryClients()
    {
        IConfiguration configuration = new ConfigurationBuilder().Add(FastTestConfigurations.ConfigServer).Build();

        var service = new ConfigServerDiscoveryService(configuration, NullLoggerFactory.Instance);
        await service.GetConfigServerInstancesAsync(new ConfigServerClientOptions(), TestContext.Current.CancellationToken);

        service.DiscoveryClients.Should().BeEmpty();
    }
}
