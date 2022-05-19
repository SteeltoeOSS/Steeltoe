// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Info;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Management.Endpoint.Info.Test
{
    public class EndpointServiceCollectionTest : BaseTest
    {
        [Fact]
        public void AddInfoActuator_AddsCorrectServices()
        {
            var services = new ServiceCollection();
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/management",
                ["management:endpoints:info:enabled"] = "false",
                ["management:endpoints:info:id"] = "infomanagement"
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var config = configurationBuilder.Build();

            services.AddInfoActuator(config);

            IInfoContributor extra = new TestInfoContributor();
            services.AddSingleton(extra);
            ILogger<InfoEndpoint> logger = new TestLogger();
            services.AddSingleton(logger);

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IInfoOptions>();
            Assert.NotNull(options);
            var contribs = serviceProvider.GetServices<IInfoContributor>();

            Assert.NotNull(contribs);
            var listOfContribs = contribs.ToList();
            Assert.Equal(4, listOfContribs.Count);

            Assert.Contains(contribs, (item) =>
                item.GetType() == typeof(GitInfoContributor) ||
                item.GetType() == typeof(AppSettingsInfoContributor) ||
                item.GetType() == typeof(BuildInfoContributor) ||
                item.GetType() == typeof(TestInfoContributor));

            var ep = serviceProvider.GetService<InfoEndpoint>();
            Assert.NotNull(ep);
        }
    }
}
