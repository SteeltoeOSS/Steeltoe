// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0_OR_GREATER
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    public partial class ConfigServerHostedServiceTest
    {
        [Fact]
        public async Task ServiceConstructsAndOperatesWithConfigurationManager()
        {
            var configurationManager = new ConfigurationManager();
            configurationManager.AddInMemoryCollection(TestHelpers._fastTestsConfiguration);
            configurationManager.AddConfigServer();
            var service = new ConfigServerHostedService(configurationManager, null);

            var startStopAction = async () =>
            {
                await service.StartAsync(default);
                await service.StopAsync(default);
            };

            await startStopAction.Should().NotThrowAsync("ConfigServerHostedService should start");
        }
    }
}
#endif
