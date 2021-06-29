﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Kubernetes.Test
{
    public class HostBuilderExtensionsTest
    {
        [Fact]
        public async Task AddKubernetesActuators_IHostBuilder_AddsAndActivatesActuators()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

            // Act
            var host = await hostBuilder.AddKubernetesActuators().StartAsync();
            var testClient = host.GetTestServer().CreateClient();

            // Assert
            var response = await testClient.GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health/liveness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"LivenessState\":\"CORRECT\"", await response.Content.ReadAsStringAsync());
            response = await testClient.GetAsync("/actuator/health/readiness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await response.Content.ReadAsStringAsync());
            response = await testClient.GetAsync("/actuator/httptrace");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddKubernetesActuators_IHostBuilder_AddsAndActivatesActuators_MediaV1()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithRouting);

            // Act
            var host = await hostBuilder.AddKubernetesActuators(MediaTypeVersion.V1).StartAsync();
            var testClient = host.GetTestServer().CreateClient();

            // Assert
            var response = await testClient.GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health/liveness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"LivenessState\":\"CORRECT\"", await response.Content.ReadAsStringAsync());
            response = await testClient.GetAsync("/actuator/health/readiness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await response.Content.ReadAsStringAsync());
            response = await testClient.GetAsync("/actuator/trace");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddKubernetesActuatorsWithConventions_IHostBuilder_AddsAndActivatesActuatorsAddAllActuators()
        {
            // Arrange
            var hostBuilder = new HostBuilder().ConfigureWebHost(_testServerWithSecureRouting);

            // Act
            var host = await hostBuilder.AddKubernetesActuators(ep => ep.RequireAuthorization("TestAuth")).StartAsync();
            var testClient = host.GetTestServer().CreateClient();

            // Assert
            var response = await testClient.GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health/liveness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"LivenessState\":\"CORRECT\"", await response.Content.ReadAsStringAsync());
            response = await testClient.GetAsync("/actuator/health/readiness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await response.Content.ReadAsStringAsync());
            response = await testClient.GetAsync("/actuator/httptrace");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddKubernetesActuators_IWebHostBuilder_AddsAndActivatesActuators()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder();
            _testServerWithRouting.Invoke(hostBuilder);

            // Act
            var host = hostBuilder.AddKubernetesActuators().Start();
            var testClient = host.GetTestServer().CreateClient();

            // Assert
            var response = await testClient.GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health/liveness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"LivenessState\":\"CORRECT\"", await response.Content.ReadAsStringAsync());
            response = await testClient.GetAsync("/actuator/health/readiness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await response.Content.ReadAsStringAsync());
            response = await testClient.GetAsync("/actuator/httptrace");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddKubernetesActuators_IWebHostBuilder_AddsAndActivatesActuators_MediaV1()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder();
            _testServerWithRouting.Invoke(hostBuilder);

            // Act
            var host = hostBuilder.AddKubernetesActuators(MediaTypeVersion.V1).Start();
            var testClient = host.GetTestServer().CreateClient();

            // Assert
            var response = await testClient.GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health/liveness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"LivenessState\":\"CORRECT\"", await response.Content.ReadAsStringAsync());
            response = await testClient.GetAsync("/actuator/health/readiness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await response.Content.ReadAsStringAsync());
            response = await testClient.GetAsync("/actuator/trace");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddKubernetesActuatorsWithConventions_IWebHostBuilder_AddsAndActivatesActuatorsAddAllActuators()
        {
            // Arrange
            var hostBuilder = new WebHostBuilder();
            _testServerWithSecureRouting.Invoke(hostBuilder);

            // Act
            var host = hostBuilder.AddKubernetesActuators(ep => ep.RequireAuthorization("TestAuth")).Start();
            var testClient = host.GetTestServer().CreateClient();

            // Assert
            var response = await testClient.GetAsync("/actuator");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/info");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            response = await testClient.GetAsync("/actuator/health/liveness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"LivenessState\":\"CORRECT\"", await response.Content.ReadAsStringAsync());
            response = await testClient.GetAsync("/actuator/health/readiness");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("\"ReadinessState\":\"ACCEPTING_TRAFFIC\"", await response.Content.ReadAsStringAsync());
            response = await testClient.GetAsync("/actuator/httptrace");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private readonly Action<IWebHostBuilder> _testServerWithRouting = builder => builder.UseTestServer().ConfigureServices(s => s.AddRouting()).Configure(a => a.UseRouting());
        private readonly Action<IWebHostBuilder> _testServerWithSecureRouting =
            builder => builder.UseTestServer()
                .ConfigureServices(s =>
                {
                    s.AddRouting();
                    s.AddAuthentication(TestAuthHandler.AuthenticationScheme)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.AuthenticationScheme, options => { });
                    s.AddAuthorization(options => options.AddPolicy("TestAuth", policy => policy.RequireClaim("scope", "actuators.read")));
                })
                .Configure(a => a.UseRouting().UseAuthentication().UseAuthorization());
    }
}
