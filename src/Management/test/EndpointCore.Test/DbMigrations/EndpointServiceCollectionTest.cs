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

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Management.Endpoint.DbMigrations.Test
{
    public class EndpointServiceCollectionTest : BaseTest
    {
        [Fact]
        public void AddEntityFrameworkActuator_ThrowsOnNulls()
        {
            var services = new ServiceCollection();
            ServiceCollection nullServices = null;
            nullServices.Invoking(s => s.AddDbMigrationsActuator(null))
                .Should()
                .Throw<ArgumentNullException>()
                .Where(x => x.ParamName == "services");
            services.Invoking(s => s.AddDbMigrationsActuator(null))
                .Should()
                .Throw<ArgumentNullException>()
                .Where(x => x.ParamName == "config");
        }

        [Fact]
        public void AddEntityFrameworkActuator_AddsCorrectServices()
        {
            var services = new ServiceCollection();

            var appSettings = new Dictionary<string, string>()
            {
                ["management:endpoints:enabled"] = "false",
                ["management:endpoints:path"] = "/cloudfoundryapplication"
            };
            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            var config = configurationBuilder.Build();
            services.AddSingleton<IConfiguration>(config);

            services.AddDbMigrationsActuator(config);

            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IDbMigrationsOptions>();
            options.Should().NotBeNull();
            var ep = serviceProvider.GetService<DbMigrationsEndpoint>();
            ep.Should().NotBeNull();
        }
    }
}
