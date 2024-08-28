// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health;

internal sealed class PostConfigureHealthCheckServiceOptionsForTest : IPostConfigureOptions<HealthCheckServiceOptions>
{
    private static readonly HealthCheckRegistration TestRegistration = new("test-registration", _ => new TestHealthCheck(), HealthStatus.Unhealthy,
    [
        "test-tag-1",
        "test-tag-2"
    ]);

    public void PostConfigure(string? name, HealthCheckServiceOptions options)
    {
        options.Registrations.Clear();
        options.Registrations.Add(TestRegistration);
    }
}
