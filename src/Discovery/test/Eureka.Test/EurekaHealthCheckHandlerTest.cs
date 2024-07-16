// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RichardSzalay.MockHttp;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.Http.HttpClientPooling;
using Steeltoe.Common.TestResources;
using Steeltoe.Discovery.Eureka.AppInfo;
using HealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;
using HealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaHealthCheckHandlerTest
{
    [Theory]
    [InlineData(HealthStatus.Down, InstanceStatus.Down)]
    [InlineData(HealthStatus.Up, InstanceStatus.Up)]
    [InlineData(HealthStatus.Warning, InstanceStatus.Up)]
    [InlineData(HealthStatus.Unknown, InstanceStatus.Unknown)]
    [InlineData(HealthStatus.OutOfService, InstanceStatus.OutOfService)]
    [InlineData(null, InstanceStatus.Up)]
    public async Task Maps_contributor_status_to_instance_status(HealthStatus? contributorStatus, InstanceStatus expectedStatus)
    {
        var contributor = new TestHealthContributor(contributorStatus);

        var services = new ServiceCollection();
        services.AddSingleton<IHealthContributor>(contributor);
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        var optionsMonitor = new TestOptionsMonitor<HealthCheckServiceOptions>();
        var handler = new EurekaHealthCheckHandler(new DefaultHealthAggregator(), optionsMonitor, serviceProvider);
        InstanceStatus result = await handler.GetStatusAsync(false, CancellationToken.None);

        result.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task Instance_status_is_up_when_no_health_contributors()
    {
        var services = new ServiceCollection();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        var optionsMonitor = new TestOptionsMonitor<HealthCheckServiceOptions>();
        var handler = new EurekaHealthCheckHandler(new DefaultHealthAggregator(), optionsMonitor, serviceProvider);
        InstanceStatus result = await handler.GetStatusAsync(false, CancellationToken.None);

        result.Should().Be(InstanceStatus.Up);
    }

    [Fact]
    public async Task Instance_status_is_unknown_when_health_contributor_throws()
    {
        var contributor = new TestHealthContributor(new Exception("Health check failed."));

        var services = new ServiceCollection();
        services.AddSingleton<IHealthContributor>(contributor);
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        var optionsMonitor = new TestOptionsMonitor<HealthCheckServiceOptions>();
        var handler = new EurekaHealthCheckHandler(new DefaultHealthAggregator(), optionsMonitor, serviceProvider);
        InstanceStatus result = await handler.GetStatusAsync(false, CancellationToken.None);

        result.Should().Be(InstanceStatus.Unknown);
    }

    [Theory]
    [InlineData(HealthStatus.Up, HealthStatus.Up, InstanceStatus.Up)]
    [InlineData(HealthStatus.Up, HealthStatus.Warning, InstanceStatus.Up)]
    [InlineData(HealthStatus.Up, HealthStatus.Unknown, InstanceStatus.Unknown)]
    [InlineData(HealthStatus.Up, HealthStatus.OutOfService, InstanceStatus.OutOfService)]
    [InlineData(HealthStatus.Up, HealthStatus.Down, InstanceStatus.Down)]
    [InlineData(HealthStatus.Warning, HealthStatus.Up, InstanceStatus.Up)]
    [InlineData(HealthStatus.Warning, HealthStatus.Warning, InstanceStatus.Up)]
    [InlineData(HealthStatus.Warning, HealthStatus.Unknown, InstanceStatus.Unknown)]
    [InlineData(HealthStatus.Warning, HealthStatus.OutOfService, InstanceStatus.OutOfService)]
    [InlineData(HealthStatus.Warning, HealthStatus.Down, InstanceStatus.Down)]
    [InlineData(HealthStatus.Unknown, HealthStatus.Up, InstanceStatus.Unknown)]
    [InlineData(HealthStatus.Unknown, HealthStatus.Warning, InstanceStatus.Unknown)]
    [InlineData(HealthStatus.Unknown, HealthStatus.Unknown, InstanceStatus.Unknown)]
    [InlineData(HealthStatus.Unknown, HealthStatus.OutOfService, InstanceStatus.OutOfService)]
    [InlineData(HealthStatus.Unknown, HealthStatus.Down, InstanceStatus.Down)]
    [InlineData(HealthStatus.OutOfService, HealthStatus.Up, InstanceStatus.OutOfService)]
    [InlineData(HealthStatus.OutOfService, HealthStatus.Warning, InstanceStatus.OutOfService)]
    [InlineData(HealthStatus.OutOfService, HealthStatus.Unknown, InstanceStatus.OutOfService)]
    [InlineData(HealthStatus.OutOfService, HealthStatus.OutOfService, InstanceStatus.OutOfService)]
    [InlineData(HealthStatus.OutOfService, HealthStatus.Down, InstanceStatus.Down)]
    [InlineData(HealthStatus.Down, HealthStatus.Up, InstanceStatus.Down)]
    [InlineData(HealthStatus.Down, HealthStatus.Warning, InstanceStatus.Down)]
    [InlineData(HealthStatus.Down, HealthStatus.Unknown, InstanceStatus.Down)]
    [InlineData(HealthStatus.Down, HealthStatus.OutOfService, InstanceStatus.Down)]
    [InlineData(HealthStatus.Down, HealthStatus.Down, InstanceStatus.Down)]
    public async Task Instance_status_is_aggregated_from_health_contributors(HealthStatus contributorStatus1, HealthStatus contributorStatus2,
        InstanceStatus expectedStatus)
    {
        IHealthContributor[] contributors =
        [
            new TestHealthContributor(contributorStatus1),
            new TestHealthContributor(contributorStatus2)
        ];

        var services = new ServiceCollection();
        services.AddSingleton(contributors[0]);
        services.AddSingleton(contributors[1]);
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        var optionsMonitor = new TestOptionsMonitor<HealthCheckServiceOptions>();
        var handler = new EurekaHealthCheckHandler(new DefaultHealthAggregator(), optionsMonitor, serviceProvider);
        InstanceStatus result = await handler.GetStatusAsync(false, CancellationToken.None);

        result.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task AddEurekaDiscoveryClient_uses_health_check_handler()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        var contributor = new TestHealthContributor(HealthStatus.Up);
        builder.Services.AddSingleton<IHealthContributor>(contributor);

        var appSettings = new Dictionary<string, string?>
        {
            ["Eureka:Client:ShouldFetchRegistry"] = "false",
            ["eureka:instance:leaseRenewalIntervalInSeconds"] = "1",
            ["eureka:client:eurekaServer:retryCount"] = "0",
            ["Eureka:Client:Health:CheckEnabled"] = "true",
            ["Eureka:Instance:AppName"] = "FOO",
            ["Eureka:Instance:InstanceId"] = "localhost:foo"
        };

        builder.Configuration.AddInMemoryCollection(appSettings);
        builder.Services.AddEurekaDiscoveryClient();

        var handler = new DelegateToMockHttpClientHandler();
        handler.Mock.Expect(HttpMethod.Post, "http://localhost:8761/eureka/apps/FOO").Respond(HttpStatusCode.NoContent);
        handler.Mock.Expect(HttpMethod.Put, "http://localhost:8761/eureka/apps/FOO/localhost%3Afoo").Respond("application/json", "{}");

        WebApplication app = builder.Build();

        app.Services.GetRequiredService<HttpClientHandlerFactory>().Using(handler);

        IDiscoveryClient[] discoveryClients = app.Services.GetServices<IDiscoveryClient>().ToArray();
        Assert.Single(discoveryClients);

        await Task.Delay(TimeSpan.FromSeconds(2));

        contributor.IsAwaited.Should().BeTrue();

        var infoManager = app.Services.GetRequiredService<EurekaApplicationInfoManager>();
        infoManager.Instance.Status.Should().Be(InstanceStatus.Up);

        handler.Mock.VerifyNoOutstandingExpectation();
    }

    private sealed class TestHealthContributor : IHealthContributor
    {
        private readonly HealthStatus? _status;
        private readonly Exception? _exception;

        public string Id => "Fake";
        public bool IsAwaited { get; private set; }

        public TestHealthContributor(Exception exception)
        {
            _exception = exception;
        }

        public TestHealthContributor(HealthStatus? status)
        {
            _status = status;
        }

        public async Task<HealthCheckResult?> CheckHealthAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();

            IsAwaited = true;

            if (_exception != null)
            {
                throw _exception;
            }

            if (_status == null)
            {
                return null;
            }

            return new HealthCheckResult
            {
                Status = _status.Value
            };
        }
    }
}
