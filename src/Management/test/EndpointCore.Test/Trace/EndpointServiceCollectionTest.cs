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
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Trace.Test
{
    public class EndpointServiceCollectionTest : BaseTest
    {
        [Fact]
        public void AddTraceActuator_ThrowsOnNulls()
        {
            // Arrange
            IServiceCollection services = null;
            IServiceCollection services2 = new ServiceCollection();
            IConfigurationRoot config = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddTraceActuator(services, config));
            Assert.Contains(nameof(services), ex.Message);
            var ex2 = Assert.Throws<ArgumentNullException>(() => EndpointServiceCollectionExtensions.AddTraceActuator(services2, config));
            Assert.Contains(nameof(config), ex2.Message);
        }

        [Fact]
        public void AddTraceActuator_AddsCorrectServices()
        {
            var services = new ServiceCollection();
            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication",
                ["management:endpoints:trace:enabled"] = "false",
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var config = configurationBuilder.Build();
            var listener = new DiagnosticListener("Test");
            services.AddSingleton(listener);

            services.AddTraceActuator(config);

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<ITraceOptions>();
            Assert.NotNull(options);
            var repo = serviceProvider.GetService<ITraceRepository>();
            Assert.NotNull(repo);
            var ep = serviceProvider.GetService<TraceEndpoint>();
            Assert.NotNull(ep);
            listener.Dispose();
        }
    }
}
