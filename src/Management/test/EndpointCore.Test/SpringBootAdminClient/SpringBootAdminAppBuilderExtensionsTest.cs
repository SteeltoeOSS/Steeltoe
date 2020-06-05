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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient.Test
{
    public class SpringBootAdminAppBuilderExtensionsTest
    {
        [Fact]
        public void SpringBootAdminClient_EndToEnd()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["management:endpoints:path"] = "/management",
                ["management:endpoints:health:path"] = "myhealth",
                ["URLS"] = "http://localhost:8080;https://localhost:8082",
                ["spring:boot:admin:client:url"] = "http://springbootadmin:9090",
                ["spring:application:name"] = "MySteeltoeApplication",
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var services = new ServiceCollection();
            var appLifeTime = new MyAppLifeTime();
            services.TryAddSingleton<IHostApplicationLifetime>(appLifeTime);
            services.TryAddSingleton<IConfiguration>(config);
            var provider = services.BuildServiceProvider();
            var appBuilder = new ApplicationBuilder(provider);

            var builder = new WebHostBuilder();
            builder.UseStartup<TestStartup>();

            using (var server = new TestServer(builder))
            {
                var client = server.CreateClient();
                appBuilder.RegisterWithSpringBootAdmin(config, client);

                appLifeTime.AppStartTokenSource.Cancel(); // Trigger application lifetime start

                Assert.NotNull(SpringBootAdminApplicationBuilderExtensions.RegistrationResult);
                Assert.Equal("1234567", SpringBootAdminApplicationBuilderExtensions.RegistrationResult.Id);

                appLifeTime.AppStopTokenSource.Cancel(); // Trigger application lifetime stop
            }
        }

        private class MyAppLifeTime : IHostApplicationLifetime
        {
            public CancellationTokenSource AppStartTokenSource = new CancellationTokenSource();
            public CancellationTokenSource AppStopTokenSource = new CancellationTokenSource();

            public CancellationToken ApplicationStarted => AppStartTokenSource.Token;

            public CancellationToken ApplicationStopped => AppStopTokenSource.Token;

            public CancellationToken ApplicationStopping => throw new NotImplementedException();

            public void StopApplication()
            {
                throw new NotImplementedException();
            }
        }
    }
}
