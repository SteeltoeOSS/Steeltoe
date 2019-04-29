// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Info.Contributor;
using Steeltoe.Management.Endpoint.Test;
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
            ServiceCollection services = new ServiceCollection();
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/management",
                ["management:endpoints:info:enabled"] = "false",
                ["management:endpoints:info:id"] = "infomanagement"
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var config = configurationBuilder.Build();

            services.AddInfoActuator(config);

            IInfoContributor extra = new TestInfoContributor();
            services.AddSingleton<IInfoContributor>(extra);
            ILogger<InfoEndpoint> logger = new TestLogger();
            services.AddSingleton<ILogger<InfoEndpoint>>(logger);

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IInfoOptions>();
            Assert.NotNull(options);
            var contribs = serviceProvider.GetServices<IInfoContributor>();

            Assert.NotNull(contribs);
            var listOfContribs = contribs.ToList();
            Assert.Equal(3, listOfContribs.Count);

            Assert.Contains(contribs, (item) =>
            {
                return
                item.GetType() == typeof(GitInfoContributor) ||
                item.GetType() == typeof(AppSettingsInfoContributor) ||
                item.GetType() == typeof(TestInfoContributor);
            });

            var ep = serviceProvider.GetService<InfoEndpoint>();
            Assert.NotNull(ep);
        }
    }
}
