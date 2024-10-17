// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.Hypermedia;
using Steeltoe.Management.Endpoint.Actuators.Info;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Hypermedia;

public sealed class StartupWithSecurity
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHypermediaActuator();
        services.AddInfoActuator();
    }

    public void Configure(IApplicationBuilder app)
    {
    }
}
