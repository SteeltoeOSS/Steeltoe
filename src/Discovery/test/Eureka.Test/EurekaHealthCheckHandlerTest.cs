// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.HealthChecks;
using Steeltoe.Discovery.Eureka.AppInfo;
using Xunit;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class EurekaHealthCheckHandlerTest
{
    [Fact]
    public void MapToInstanceStatus_ReturnsExpected()
    {
        var handler = new EurekaHealthCheckHandler();
        Assert.Equal(InstanceStatus.Down, handler.MapToInstanceStatus(HealthStatus.Down));
        Assert.Equal(InstanceStatus.Up, handler.MapToInstanceStatus(HealthStatus.Up));
        Assert.Equal(InstanceStatus.Unknown, handler.MapToInstanceStatus(HealthStatus.Warning));
        Assert.Equal(InstanceStatus.Unknown, handler.MapToInstanceStatus(HealthStatus.Unknown));
        Assert.Equal(InstanceStatus.OutOfService, handler.MapToInstanceStatus(HealthStatus.OutOfService));
    }

    [Fact]
    public async Task DoHealthChecks_ReturnsExpected()
    {
        var handler = new EurekaHealthCheckHandler();
        List<HealthCheckResult> result = await handler.DoHealthChecksAsync(new List<IHealthContributor>(), CancellationToken.None);
        Assert.Empty(result);

        result = await handler.DoHealthChecksAsync(new List<IHealthContributor>
        {
            new TestContributor()
        }, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public void AggregateStatus_ReturnsExpected()
    {
        var handler = new EurekaHealthCheckHandler();

        var results = new List<HealthCheckResult>();
        Assert.Equal(HealthStatus.Unknown, handler.AggregateStatus(results));

        results = new List<HealthCheckResult>
        {
            new()
            {
                Status = HealthStatus.Down
            },
            new()
            {
                Status = HealthStatus.Up
            }
        };

        Assert.Equal(HealthStatus.Down, handler.AggregateStatus(results));

        results = new List<HealthCheckResult>
        {
            new()
            {
                Status = HealthStatus.Up
            },
            new()
            {
                Status = HealthStatus.Down
            }
        };

        Assert.Equal(HealthStatus.Down, handler.AggregateStatus(results));

        results = new List<HealthCheckResult>
        {
            new()
            {
                Status = HealthStatus.Up
            },
            new()
            {
                Status = HealthStatus.OutOfService
            }
        };

        Assert.Equal(HealthStatus.OutOfService, handler.AggregateStatus(results));

        results = new List<HealthCheckResult>
        {
            new()
            {
                Status = HealthStatus.Up
            },
            new()
            {
                Status = HealthStatus.Warning
            }
        };

        Assert.Equal(HealthStatus.Up, handler.AggregateStatus(results));

        results = new List<HealthCheckResult>
        {
            new()
            {
                Status = HealthStatus.Warning
            },
            new()
            {
                Status = HealthStatus.Warning
            }
        };

        Assert.Equal(HealthStatus.Unknown, handler.AggregateStatus(results));
    }

    private sealed class TestContributor : IHealthContributor
    {
        public string Id => "TestContrib";

        public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken)
        {
            await Task.Yield();
            throw new NotImplementedException();
        }
    }
}
