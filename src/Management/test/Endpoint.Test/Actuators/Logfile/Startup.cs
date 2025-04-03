// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.Logfile;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Logfile;

public sealed class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddLogfileActuator();
    }

    public void Configure(IApplicationBuilder app)
    {
    }
}
