// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using FluentAssertions.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;
using SteeltoeHealthCheckResult = Steeltoe.Common.HealthChecks.HealthCheckResult;
using SteeltoeHealthStatus = Steeltoe.Common.HealthChecks.HealthStatus;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health;

public sealed class HealthAggregatorTest : BaseTest
{
    private static readonly ICollection<HealthCheckRegistration> EmptyHealthCheckRegistrations = Array.Empty<HealthCheckRegistration>();
    private static readonly IServiceProvider EmptyServiceProvider = new ServiceCollection().BuildServiceProvider(true);

    [Fact]
    public async Task Aggregate_EmptyContributorList_ReturnsExpectedHealth()
    {
        var aggregator = new HealthAggregator();

        SteeltoeHealthCheckResult result = await aggregator.AggregateAsync(Array.Empty<IHealthContributor>(), EmptyHealthCheckRegistrations,
            EmptyServiceProvider, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(SteeltoeHealthStatus.Unknown, result.Status);
        Assert.Empty(result.Details);
    }

    [Fact]
    public async Task Aggregate_DisabledContributorOnly_ReturnsExpectedHealth()
    {
        List<IHealthContributor> contributors = [new DisabledContributor()];
        var aggregator = new HealthAggregator();

        SteeltoeHealthCheckResult result = await aggregator.AggregateAsync(contributors, EmptyHealthCheckRegistrations, EmptyServiceProvider,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(SteeltoeHealthStatus.Unknown, result.Status);
        Assert.Empty(result.Details);
    }

    [Fact]
    public async Task Aggregate_SingleContributor_ReturnsExpectedHealth()
    {
        List<IHealthContributor> contributors = [new UpContributor()];
        var aggregator = new HealthAggregator();

        SteeltoeHealthCheckResult result = await aggregator.AggregateAsync(contributors, EmptyHealthCheckRegistrations, EmptyServiceProvider,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(SteeltoeHealthStatus.Up, result.Status);
        Assert.NotEmpty(result.Details);
    }

    [Fact]
    public async Task Aggregate_SingleCanceledContributor_Throws()
    {
        List<IHealthContributor> contributors = [new UpContributor(5.Seconds())];
        using var source = new CancellationTokenSource();

        await source.CancelAsync();

        var aggregator = new HealthAggregator();
        Func<Task> action = async () => await aggregator.AggregateAsync(contributors, EmptyHealthCheckRegistrations, EmptyServiceProvider, source.Token);

        await action.Should().ThrowExactlyAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task Aggregate_MultipleContributor_ReturnsExpectedHealth()
    {
        List<IHealthContributor> contributors =
        [
            new DownContributor(),
            new UpContributor(),
            new OutOfServiceContributor(),
            new UnknownContributor(),
            new DisabledContributor()
        ];

        var aggregator = new HealthAggregator();

        SteeltoeHealthCheckResult result = await aggregator.AggregateAsync(contributors, EmptyHealthCheckRegistrations, EmptyServiceProvider,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(SteeltoeHealthStatus.Down, result.Status);
        Assert.NotEmpty(result.Details);
    }

    [Fact]
    public async Task Aggregate_DuplicateContributor_ReturnsExpectedHealth()
    {
        List<IHealthContributor> contributors = [];

        for (int index = 0; index < 10; index++)
        {
            contributors.Add(new UpContributor());
        }

        var aggregator = new HealthAggregator();

        SteeltoeHealthCheckResult result = await aggregator.AggregateAsync(contributors, EmptyHealthCheckRegistrations, EmptyServiceProvider,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(SteeltoeHealthStatus.Up, result.Status);
        Assert.NotEmpty(result.Details);
        Assert.Contains("alwaysUp-9", result.Details.Keys);
    }

    [Fact]
    public async Task Aggregate_MultipleContributor_OrderDoesNotMatter_ReturnsExpectedHealth()
    {
        List<IHealthContributor> contributors =
        [
            new UpContributor(),
            new OutOfServiceContributor(),
            new UnknownContributor()
        ];

        var aggregator = new HealthAggregator();

        SteeltoeHealthCheckResult result = await aggregator.AggregateAsync(contributors, EmptyHealthCheckRegistrations, EmptyServiceProvider,
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(SteeltoeHealthStatus.OutOfService, result.Status);
        Assert.NotEmpty(result.Details);
    }

    [Fact]
    public async Task AggregatesContributorsInParallel()
    {
        var stopwatch = new Stopwatch();

        List<IHealthContributor> contributors =
        [
            new UpContributor(1.Seconds()),
            new UpContributor(2.Seconds()),
            new UpContributor(3.Seconds())
        ];

        var aggregator = new HealthAggregator();
        stopwatch.Start();

        SteeltoeHealthCheckResult result = await aggregator.AggregateAsync(contributors, EmptyHealthCheckRegistrations, EmptyServiceProvider,
            TestContext.Current.CancellationToken);

        stopwatch.Stop();
        Assert.NotNull(result);
        Assert.Equal(SteeltoeHealthStatus.Up, result.Status);
        Assert.InRange(stopwatch.Elapsed, 500.Milliseconds(), 5.Seconds());
    }
}
