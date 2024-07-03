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
    public void ConfigServerDiscoveryService_FindsNoDiscoveryClients()
    {
        var appSettings = new Dictionary<string, string?>(TestHelpers.FastTestsConfiguration);

        IConfigurationRoot configuration = new ConfigurationBuilder().AddInMemoryCollection(appSettings).Build();
        var options = new ConfigServerClientOptions();

        var service = new ConfigServerDiscoveryService(configuration, options, NullLoggerFactory.Instance);

        Assert.Empty(service.DiscoveryClients);
    }
}
