// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace Steeltoe.Management.Endpoint.Health.Test;

public sealed class TestServiceOptions : IOptionsMonitor<HealthCheckServiceOptions>, IDisposable
{
    public TestServiceOptions()
    {
        CurrentValue = new HealthCheckServiceOptions();
        CurrentValue.Registrations.Add(new HealthCheckRegistration("test", _ => new TestHealthCheck(), HealthStatus.Unhealthy, new[] { "tags" }.ToList()));
    }

    public HealthCheckServiceOptions CurrentValue { get; }

    public void Dispose()
    {
    }

    public HealthCheckServiceOptions Get(string name)
    {
        return CurrentValue;
    }

    public IDisposable OnChange(Action<HealthCheckServiceOptions, string> listener)
    {
        return this;
    }
}
