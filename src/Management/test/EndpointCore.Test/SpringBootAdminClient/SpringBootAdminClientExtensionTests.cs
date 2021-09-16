// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint.SpringBootAdminClient;
using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;
using IApplicationLifetime = Microsoft.Extensions.Hosting.IApplicationLifetime;

namespace Steeltoe.Management.EndpointCore.Test.SpringBootAdminClient
{
    public class SpringBootAdminClientExtensionTests
    {
        [Fact]
        public void BootAdminClient_Endtoend()
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
            var appLifeTime = AddApplicationLifetime(services);
            services.TryAddSingleton<IConfiguration>(config);
            var provider = services.BuildServiceProvider();
            var appBuilder = new MyAppBuilder(provider);
            var builder = new WebHostBuilder().UseStartup<TestStartup>();

            using var server = new TestServer(builder);
            var client = server.CreateClient();
            appBuilder.RegisterSpringBootAdmin(config, client);

            appLifeTime.AppStartTokenSource.Cancel(); // Trigger application lifetime start

            Assert.NotNull(BootAdminAppBuilderExtensions.RegistrationResult);
            Assert.Equal("1234567", BootAdminAppBuilderExtensions.RegistrationResult.Id);

            appLifeTime.AppStopTokenSource.Cancel(); // Trigger application lifetime stop
        }

        private MyAppLifeTime AddApplicationLifetime(ServiceCollection services)
        {
            var appLifeTime = new MyAppLifeTime();
            services.TryAddSingleton<IHostApplicationLifetime>(appLifeTime);
            return appLifeTime;
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

        private class MyAppBuilder : IApplicationBuilder
        {
            public MyAppBuilder(IServiceProvider provider)
            {
                ApplicationServices = provider;
            }

            public IServiceProvider ApplicationServices { get; set; }

            public IFeatureCollection ServerFeatures => throw new NotImplementedException();

            public IDictionary<string, object> Properties => throw new NotImplementedException();

            public RequestDelegate Build()
            {
                throw new NotImplementedException();
            }

            public IApplicationBuilder New()
            {
                throw new NotImplementedException();
            }

            public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
            {
                throw new NotImplementedException();
            }
        }
    }
}
