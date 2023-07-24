// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Health;
using Steeltoe.Management.Endpoint.Health.Contributor;
using Steeltoe.Management.Endpoint.Test.Health.TestContributors;

namespace Steeltoe.Management.Endpoint.Test.Health;

public sealed class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();

        switch (_configuration.GetValue<string>("HealthCheckType"))
        {
            case "down":
                services.AddHealthActuator(typeof(DownContributor));
                break;
            case "out":
                services.AddHealthActuator(typeof(OutOfServiceContributor));
                break;
            case "unknown":
                services.AddHealthActuator(typeof(UnknownContributor));
                break;
            case "defaultAggregator":
                services.AddHealthActuator(new DefaultHealthAggregator(), typeof(DiskSpaceContributor));
                break;
            case "microsoftHealthAggregator":
                services.AddSingleton<IOptionsMonitor<HealthCheckServiceOptions>>(new TestServiceOptions());
                services.AddHealthActuator(new HealthRegistrationsAggregator(), typeof(DiskSpaceContributor));
                break;
            default:
                services.AddHealthActuator();
                break;
        }
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints => endpoints.MapAllActuators());
    }
}
