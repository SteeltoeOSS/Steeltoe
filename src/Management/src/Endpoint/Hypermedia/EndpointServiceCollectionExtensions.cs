// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Extensions;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Hypermedia;

public static class EndpointServiceCollectionExtensions
{
    public static void AddHypermediaActuator(this IServiceCollection services, IConfiguration configuration = null)
    {
        ArgumentGuard.NotNull(services);

        configuration ??= services.BuildServiceProvider().GetRequiredService<IConfiguration>();

        services.AddActuatorManagementOptions(configuration);
        services.AddHypermediaActuatorServices(configuration);
        services.AddActuatorEndpointMapping<ActuatorEndpoint>();

    }
    
    public static void AddActuatorManagementOptions(this IServiceCollection services, IConfiguration configuration = null)
    {
        ArgumentGuard.NotNull(services);
        
        configuration ??= services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        //services.Configure<ManagementEndpointOptions>(ManagementEndpointOptions.ActuatorOptionName, configuration.GetSection(ManagementEndpointOptions.ManagementInfoPrefix)).c;
        services.AddOptions<ManagementEndpointOptions>(ManagementEndpointOptions.ActuatorOptionName)
            .Configure(managementOptions =>
            {
                configuration.GetSection(ManagementEndpointOptions.ManagementInfoPrefix).Bind(managementOptions);
                managementOptions.Path = ManagementEndpointOptions.DefaultActuatorPath;
            });
        services.AddOptions<ManagementEndpointOptions>(ManagementEndpointOptions.CFOptionName)
           .Configure(managementOptions =>
           {
               configuration.GetSection(ManagementEndpointOptions.ManagementInfoPrefix).Bind(managementOptions);
               managementOptions.Path = ManagementEndpointOptions.DefaultCFPath;
           });
       // services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new ActuatorManagementOptions(configuration)));
     //   services.TryAddSingleton(provider => provider.GetServices<IManagementOptions>().OfType<ActuatorManagementOptions>().First());
    }
}
