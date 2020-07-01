// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Extensions.Logging.DynamicLogger;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Xunit;

namespace Steeltoe.Management.Endpoint
{
    public class ActuatorRouteBuilderExtensionsTest
    {
        public static IEnumerable<object[]> IEndpointImplementations
        {
            get
            {
                Func<Type, bool> query = t => t.GetInterfaces().Any(type => type.FullName == "Steeltoe.Management.IEndpoint");
                var types = Assembly.Load("Steeltoe.Management.EndpointBase").GetTypes().Where(query).ToList();
                types.AddRange(Assembly.Load("Steeltoe.Management.EndpointCore").GetTypes().Where(query));
                return types.Select(t => new object[] { t });
            }
        }

        public static IEnumerable<object[]> IEndpointImplementationsWithAuth
        {
            get
            {
                Func<Type, bool> query = t => t.GetInterfaces().Any(type => type.FullName == "Steeltoe.Management.IEndpoint");
                var types = Assembly.Load("Steeltoe.Management.EndpointBase").GetTypes().Where(query).ToList();
                types.AddRange(Assembly.Load("Steeltoe.Management.EndpointCore").GetTypes().Where(query));
                var auths = new bool[] { true, false };
                return from t in types
                       from a in auths
                       select new object[] { t, a };
            }
        }

        [Theory]
        [MemberData(nameof(IEndpointImplementations))]
        public void LookupMiddlewareTest(Type type)
        {
            var (middleware, options) = ActuatorRouteBuilderExtensions.LookupMiddleware(type);
            Assert.NotNull(middleware);
            Assert.NotNull(options);
        }

        [Theory]
        [MemberData(nameof(IEndpointImplementationsWithAuth))]
        public async void MapTestAuthSuccess(Type type, bool authSuccess)
        {
            // Arrange
            ServiceProvider provider = null;
            Action<AuthorizationPolicyBuilder> policyAction = policy => policy.RequireClaim("scope", "invalidscope");

            if (authSuccess)
            {
                policyAction = policy => policy.RequireClaim("scope", "actuators.read"); // Set up for success
            }

            var hostBuilder = new HostBuilder().ConfigureWebHost(builder =>
                    builder.UseTestServer()
                    .ConfigureServices((context, s) =>
                    {
                        s.AddRouting();
                        s.AddTraceActuator(context.Configuration, MediaTypeVersion.V1);
                        s.AddThreadDumpActuator(context.Configuration, MediaTypeVersion.V1);
                        s.AddCloudFoundryActuator(context.Configuration);
                        s.AddAllActuators(context.Configuration); // Add all of them, but map one at a time
                        s.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
                        s.AddAuthorization(options => options.AddPolicy("TestAuth", policyAction)); // setup Auth based on test Case
                        provider = s.BuildServiceProvider();
                    })
                    .Configure(a => a.UseRouting().UseAuthentication().UseAuthorization().UseEndpoints(endpoints => endpoints.MapActuatorEndpoint(type).RequireAuthorization("TestAuth"))));

            // Act
            var host = await hostBuilder.AddDynamicLogging().StartAsync();

            // Assert
            var (middleware, optionsType) = ActuatorRouteBuilderExtensions.LookupMiddleware(type);
            var options = provider.GetService(optionsType) as IEndpointOptions;
            var mgmtContext = type.IsAssignableFrom(typeof(CloudFoundryEndpoint))
                ? (IManagementOptions)provider.GetRequiredService<CloudFoundryManagementOptions>()
                : (IManagementOptions)provider.GetRequiredService<ActuatorManagementOptions>();
            var path = options.GetContextPath(mgmtContext);
            Assert.NotNull(path);

            var response = host.GetTestServer().CreateClient().GetAsync(path);
            var expected = authSuccess ? HttpStatusCode.OK : HttpStatusCode.Unauthorized;

            Assert.True(expected == response.Result.StatusCode, $"Expected {expected}, but got {response.Result.StatusCode} for {path} and type {type}");
        }
    }
}
