// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Actuators.Health;
using Steeltoe.Management.Endpoint.Test.Actuators.Health.TestContributors;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Health;

public sealed class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHealthActuator();

        switch (_configuration.GetValue<string?>("HealthCheckType"))
        {
            case "down":
                services.RemoveAll<IHealthContributor>();
                services.AddHealthContributor<DownContributor>();
                break;
            case "out":
                services.RemoveAll<IHealthContributor>();
                services.AddHealthContributor<OutOfServiceContributor>();
                break;
            case "unknown":
                services.RemoveAll<IHealthContributor>();
                services.AddHealthContributor<UnknownContributor>();
                break;
            case "disabled":
                services.RemoveAll<IHealthContributor>();
                services.AddHealthContributor<DisabledContributor>();
                break;
            case "default":
                services.TryAddEnumerable(ServiceDescriptor
                    .Singleton<IPostConfigureOptions<HealthCheckServiceOptions>, PostConfigureHealthCheckServiceOptionsForTest>());

                break;
        }
    }

    public void Configure(IApplicationBuilder app)
    {
    }
}
