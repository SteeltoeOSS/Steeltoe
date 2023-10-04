// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using FluentAssertions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Test.Health.TestContributors;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Health;

public sealed class DefaultHealthAggregatorTest : BaseTest
{
    [Fact]
    public async Task Aggregate_EmptyContributorList_ReturnsExpectedHealth()
    {
        var aggregator = new DefaultHealthAggregator();
        HealthCheckResult result = await aggregator.AggregateAsync(Array.Empty<IHealthContributor>(), CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Unknown, result.Status);
        Assert.NotNull(result.Details);
    }

    [Fact]
    public async Task Aggregate_DisabledContributorOnly_ReturnsExpectedHealth()
    {
        var contributors = new List<IHealthContributor>
        {
            new DisabledContributor()
        };

        var aggregator = new DefaultHealthAggregator();
        HealthCheckResult result = await aggregator.AggregateAsync(contributors, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Unknown, result.Status);
        Assert.NotNull(result.Details);
    }

    [Fact]
    public async Task Aggregate_SingleContributor_ReturnsExpectedHealth()
    {
        var contributors = new List<IHealthContributor>
        {
            new UpContributor()
        };

        var aggregator = new DefaultHealthAggregator();
        HealthCheckResult result = await aggregator.AggregateAsync(contributors, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.NotNull(result.Details);
    }

    [Fact]
    public async Task Aggregate_SingleCanceledContributor_Throws()
    {
        var contributors = new List<IHealthContributor>
        {
            new UpContributor(5000)
        };

        var source = new CancellationTokenSource();
        source.Cancel();

        var aggregator = new DefaultHealthAggregator();
        Func<Task> action = async () => await aggregator.AggregateAsync(contributors, source.Token);

        await action.Should().ThrowExactlyAsync<TaskCanceledException>();
    }

    [Fact]
    public async Task Aggregate_MultipleContributor_ReturnsExpectedHealth()
    {
        var contributors = new List<IHealthContributor>
        {
            new DownContributor(),
            new UpContributor(),
            new OutOfServiceContributor(),
            new UnknownContributor(),
            new DisabledContributor()
        };

        var aggregator = new DefaultHealthAggregator();
        HealthCheckResult result = await aggregator.AggregateAsync(contributors, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Down, result.Status);
        Assert.NotNull(result.Details);
    }

    [Fact]
    public async Task Aggregate_DuplicateContributor_ReturnsExpectedHealth()
    {
        var contributors = new List<IHealthContributor>();

        for (int index = 0; index < 10; index++)
        {
            contributors.Add(new UpContributor());
        }

        var aggregator = new DefaultHealthAggregator();
        HealthCheckResult result = await aggregator.AggregateAsync(contributors, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.Contains("Up-9", result.Details.Keys);
    }

    [Fact]
    public async Task Aggregate_MultipleContributor_OrderDoesNotMatter_ReturnsExpectedHealth()
    {
        var contributors = new List<IHealthContributor>
        {
            new UpContributor(),
            new OutOfServiceContributor(),
            new UnknownContributor()
        };

        var aggregator = new DefaultHealthAggregator();
        HealthCheckResult result = await aggregator.AggregateAsync(contributors, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.OutOfService, result.Status);
        Assert.NotNull(result.Details);
    }

    [Fact]
    public async Task AggregatesInParallel()
    {
        var stopwatch = new Stopwatch();

        var contributors = new List<IHealthContributor>
        {
            new UpContributor(500),
            new UpContributor(500),
            new UpContributor(500)
        };

        var aggregator = new DefaultHealthAggregator();
        stopwatch.Start();
        HealthCheckResult result = await aggregator.AggregateAsync(contributors, CancellationToken.None);
        stopwatch.Stop();
        Assert.NotNull(result);
        Assert.Equal(HealthStatus.Up, result.Status);
        Assert.InRange(stopwatch.ElapsedMilliseconds, 450, 1200);
    }
}
