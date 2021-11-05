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
using Steeltoe.Common;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.Endpoint.CloudFoundry;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.Endpoint.ThreadDump;
using Steeltoe.Management.Endpoint.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using EndpointServiceCollectionExtensions = Steeltoe.Management.Endpoint.HeapDump.EndpointServiceCollectionExtensions;

namespace Steeltoe.Management.Endpoint
{
    public class ActuatorRouteBuilderExtensionsTest
    {
        public static IEnumerable<object[]> IEndpointImplementations
        {
            get
            {
                static bool Query(Type t) => t.GetInterfaces().Any(type => type.FullName == "Steeltoe.Management.IEndpoint");
                var types = Assembly.Load("Steeltoe.Management.EndpointBase").GetTypes().Where(Query).ToList();
                types.AddRange(Assembly.Load("Steeltoe.Management.EndpointCore").GetTypes().Where(Query));
                return types.Select(t => new object[] { t });
            }
        }

        public static IEnumerable<object[]> IEndpointImplementationsForCurrentPlatform
        {
            get
            {
                static bool Query(Type t) => t.GetInterfaces().Any(type => type.FullName == "Steeltoe.Management.IEndpoint");
                static bool SupportedOnPlatform(Type t) => !(t.Name.StartsWith("ThreadDump") || t.Name.StartsWith("HeapDump"))
                                                            || (t.Name.StartsWith("ThreadDump") && Platform.IsWindows)
                                                            || (t.Name.StartsWith("HeapDump") && EndpointServiceCollectionExtensions.IsHeapDumpSupported());
                var types = Assembly.Load("Steeltoe.Management.EndpointBase").GetTypes()
                                .Union(Assembly.Load("Steeltoe.Management.EndpointCore").GetTypes())
                                .Where(Query).Where(SupportedOnPlatform).ToList();
                return from t in types select new object[] { t };
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
        [MemberData(nameof(IEndpointImplementationsForCurrentPlatform))]
        public async Task MapTestAuthSuccess(Type type)
        {
            var hostBuilder = GetHostBuilder(type, policy => policy.RequireClaim("scope", "actuators.read"));
            await ActAndAssert(type, hostBuilder, true);
        }

        [Theory]
        [MemberData(nameof(IEndpointImplementationsForCurrentPlatform))]
        public async Task MapTestAuthFail(Type type)
        {
            var hostBuilder = GetHostBuilder(type, policy => policy.RequireClaim("scope", "invalidscope"));
            await ActAndAssert(type, hostBuilder, false);
        }

        private static IHostBuilder GetHostBuilder(Type type, Action<AuthorizationPolicyBuilder> policyAction)
            => new HostBuilder()
                .AddDynamicLogging()
                .ConfigureServices((context, s) =>
                {
                    s.AddTraceActuator(context.Configuration, MediaTypeVersion.V1);
                    s.AddThreadDumpActuator(context.Configuration, MediaTypeVersion.V1);
                    s.AddCloudFoundryActuator(context.Configuration);
                    s.AddAllActuators(context.Configuration); // Add all of them, but map one at a time
                    s.AddRouting();
                    s.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
                    s.AddAuthorization(options => options.AddPolicy("TestAuth", policyAction)); // setup Auth based on test Case
                    s.AddServerSideBlazor();
                })
                .ConfigureWebHost(builder =>
                {
                    builder
                        .Configure(app =>
                            app
                                .UseRouting()
                                .UseAuthentication()
                                .UseAuthorization()
                                .UseEndpoints(endpoints =>
                                {
                                    endpoints.MapBlazorHub(); // https://github.com/SteeltoeOSS/Steeltoe/issues/729
#if NET6_0
                                    endpoints.MapActuatorEndpoint(type, convention => convention.RequireAuthorization("TestAuth"));
#else
#pragma warning disable CS0618 // Type or member is obsolete
                                    endpoints.MapActuatorEndpoint(type).RequireAuthorization("TestAuth");
#pragma warning restore CS0618 // Type or member is obsolete
#endif

                                }))
                        .UseTestServer();
                });

        private static IManagementOptions GetManagementContext(Type type, IServiceProvider services)
            => type.IsAssignableFrom(typeof(CloudFoundryEndpoint))
                ? services.GetRequiredService<CloudFoundryManagementOptions>()
                : services.GetRequiredService<ActuatorManagementOptions>();

        private async Task ActAndAssert(Type type, IHostBuilder hostBuilder, bool expectedSuccess)
        {
            var host = await hostBuilder.StartAsync();
            var (middleware, optionsType) = ActuatorRouteBuilderExtensions.LookupMiddleware(type);
            var options = host.Services.GetService(optionsType) as IEndpointOptions;
            var path = options.GetContextPath(GetManagementContext(type, host.Services));

            Assert.NotNull(path);

            using var server = host.GetTestServer();
            var response = await server.CreateClient().GetAsync(path);

            Assert.True(expectedSuccess == response.IsSuccessStatusCode, $"Expected {(expectedSuccess ? "success" : "failure")}, but got {response.StatusCode} for {path} and type {type}");
        }
    }
}
