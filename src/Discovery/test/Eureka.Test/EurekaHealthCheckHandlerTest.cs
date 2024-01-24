// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.Discovery;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Eureka.AppInfo;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaHealthCheckHandlerTest
{
    [Theory]
    [InlineData(HealthStatus.Down, InstanceStatus.Down)]
    [InlineData(HealthStatus.Up, InstanceStatus.Up)]
    [InlineData(HealthStatus.Warning, InstanceStatus.Unknown)]
    [InlineData(HealthStatus.Unknown, InstanceStatus.Unknown)]
    [InlineData(HealthStatus.OutOfService, InstanceStatus.OutOfService)]
    [InlineData(null, InstanceStatus.Unknown)]
    public async Task Maps_contributor_status_to_instance_status(HealthStatus? contributorStatus, InstanceStatus expectedStatus)
    {
        var contributor = new TestHealthContributor(contributorStatus);
        var handler = new EurekaHealthCheckHandler([contributor], NullLogger<EurekaHealthCheckHandler>.Instance);

        InstanceStatus result = await handler.GetStatusAsync(CancellationToken.None);

        result.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task Instance_status_is_unknown_when_no_health_contributors()
    {
        var handler = new EurekaHealthCheckHandler([], NullLogger<EurekaHealthCheckHandler>.Instance);

        InstanceStatus result = await handler.GetStatusAsync(CancellationToken.None);

        result.Should().Be(InstanceStatus.Unknown);
    }

    [Fact]
    public async Task Instance_status_is_unknown_when_health_contributor_throws()
    {
        var contributor = new TestHealthContributor(new Exception("Health check failed."));
        var handler = new EurekaHealthCheckHandler([contributor], NullLogger<EurekaHealthCheckHandler>.Instance);

        InstanceStatus result = await handler.GetStatusAsync(CancellationToken.None);

        result.Should().Be(InstanceStatus.Unknown);
    }

    [Theory]
    [InlineData(HealthStatus.Down, HealthStatus.Up, InstanceStatus.Down)]
    [InlineData(HealthStatus.Up, HealthStatus.Down, InstanceStatus.Down)]
    [InlineData(HealthStatus.Up, HealthStatus.OutOfService, InstanceStatus.OutOfService)]
    [InlineData(HealthStatus.Up, HealthStatus.Warning, InstanceStatus.Up)]
    [InlineData(HealthStatus.Warning, HealthStatus.Warning, InstanceStatus.Unknown)]
    public async Task Instance_status_is_aggregated_from_health_contributors(HealthStatus contributorStatus1, HealthStatus contributorStatus2,
        InstanceStatus expectedStatus)
    {
        IHealthContributor[] contributors =
        [
            new TestHealthContributor(contributorStatus1),
            new TestHealthContributor(contributorStatus2)
        ];

        var handler = new EurekaHealthCheckHandler(contributors, NullLogger<EurekaHealthCheckHandler>.Instance);

        InstanceStatus result = await handler.GetStatusAsync(CancellationToken.None);

        result.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task AddServiceDiscovery_uses_health_check_handler()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();

        var contributor = new TestHealthContributor(HealthStatus.Up);
        builder.Services.AddSingleton<IHealthContributor>(contributor);

        var eurekaSettings = new Dictionary<string, string?>
        {
            ["eureka:instance:leaseRenewalIntervalInSeconds"] = "1",
            ["eureka:client:eurekaServer:retryCount"] = "0"
        };

        builder.Configuration.AddInMemoryCollection(eurekaSettings);
        builder.Services.AddServiceDiscovery(builder.Configuration, options => options.UseEureka());
        builder.Services.AddSingleton<IHealthCheckHandler, EurekaHealthCheckHandler>();

        WebApplication app = builder.Build();

        _ = app.Services.GetRequiredService<IDiscoveryClient>();
        await Task.Delay(TimeSpan.FromSeconds(2));

        contributor.IsAwaited.Should().BeTrue();

        var infoManager = app.Services.GetRequiredService<EurekaApplicationInfoManager>();
        infoManager.InstanceStatus.Should().Be(InstanceStatus.Up);
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
