// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common;
using Steeltoe.Messaging.RabbitMQ.Config;

namespace Steeltoe.Messaging.RabbitMQ.Extensions;

public static class RabbitListenerExtensions
{
    public static IServiceCollection AddRabbitListeners(this IServiceCollection services, IConfiguration config, params Type[] listenerServices)
    {
        ArgumentGuard.NotNull(services);

        foreach (Type t in listenerServices)
        {
            var metadata = RabbitListenerMetadata.BuildMetadata(services, t);

            if (metadata != null)
            {
                services.AddSingleton(metadata);
            }

            RabbitListenerDeclareAttributeProcessor.ProcessDeclareAttributes(services, config, t);
        }

        return services;
    }

    public static IServiceCollection AddRabbitListeners<T>(this IServiceCollection services, IConfiguration config = null)
        where T : class
    {
        return services.AddRabbitListeners(config, typeof(T));
    }
}
