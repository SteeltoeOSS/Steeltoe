// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.RouteMappings;
using Steeltoe.Management.Endpoint.Web.Hypermedia;

namespace Steeltoe.Management.Endpoint.Test.ContentNegotiation;

public sealed class MappingsStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();
        services.AddActionDescriptorCollectionProviderMock();
        services.AddHypermediaActuator();
        services.AddMappingsActuator();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapAllActuators();
        });
    }
}
