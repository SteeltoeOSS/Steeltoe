// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health;

internal sealed class TestHealthCheckServiceOptions : IOptionsMonitor<HealthCheckServiceOptions>, IDisposable
{
    public HealthCheckServiceOptions CurrentValue { get; }

    public TestHealthCheckServiceOptions()
    {
        CurrentValue = new HealthCheckServiceOptions();

        CurrentValue.Registrations.Add(new HealthCheckRegistration("test-registration", _ => new TestHealthCheck(), HealthStatus.Unhealthy, new[]
        {
            "test-tag-1",
            "test-tag-2"
        }.ToList()));
    }

    public void Dispose()
    {
    }

    public HealthCheckServiceOptions Get(string? name)
    {
        return CurrentValue;
    }

    public IDisposable OnChange(Action<HealthCheckServiceOptions, string?> listener)
    {
        return this;
    }
}
