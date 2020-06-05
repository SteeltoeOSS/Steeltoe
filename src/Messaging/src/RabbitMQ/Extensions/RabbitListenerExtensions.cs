// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Messaging.Rabbit.Config;
using System;

namespace Steeltoe.Messaging.Rabbit.Extensions
{
    public static class RabbitListenerExtensions
    {
        public static IServiceCollection AddRabbitListeners<T>(this IServiceCollection services)
            where T : class
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var metadata = RabbitListenerMetadata.BuildMetadata(typeof(T));
            if (metadata != null)
            {
                services.AddSingleton(metadata);
                services.AddSingleton<T>();
            }

            return services;
        }
    }
}
