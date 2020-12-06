// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

﻿using Microsoft.AspNetCore.Builder;
﻿using Microsoft.AspNetCore.Hosting;
﻿using Microsoft.Extensions.DependencyInjection.Extensions;
﻿using Steeltoe.Management;
﻿using Steeltoe.Management.Endpoint;
﻿using Steeltoe.Management.Endpoint.Internal;
﻿using System;

﻿namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers an endpoint that gets mapped when calling <see cref="ActuatorRouteBuilderExtensions.MapAllActuators(AspNetCore.Routing.IEndpointRouteBuilder)"/>
        /// </summary>
        /// <typeparam name="TEndpoint">The type of endpoint</typeparam>
        /// <param name="services">The service collection</param>
        /// <returns>A reference to the service collection</returns>
        public static IServiceCollection AddActuatorEndpointEntry<TEndpoint>(this IServiceCollection services)
            where TEndpoint : class, IEndpoint
        {
            services.TryAddSingleton<TEndpoint>();

            services.TryAddSingleton<EndpointTracking>();
            services.AddSingleton(new EndpointEntry
            {
                Name = typeof(TEndpoint).Name,
                Setup = (endpoints, convention) => endpoints.MapInternal<TEndpoint>(convention)
            });
            return services;
        }

        /// <summary>
        /// Registers an <see cref="IStartupFilter"/> that maps all endpoints registered with <see cref="AddActuatorEndpointEntry{TEndpoint}(IServiceCollection)"/>
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureEndpoints">Endpoint configuration builder</param>
        /// <returns>A reference to the service collection</returns>
        public static IServiceCollection AddActuatorStartupFilter(this IServiceCollection services, Action<IEndpointConventionBuilder> configureEndpoints = null)
        {
            services.TryAdd(ServiceDescriptor.Transient<IStartupFilter>(sp => new AllActuatorsStartupFilter(configureEndpoints)));

            return services;
        }
    }
}
