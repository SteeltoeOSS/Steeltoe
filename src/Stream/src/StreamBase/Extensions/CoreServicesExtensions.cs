// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.Core;

namespace Steeltoe.Stream.Extensions
{
    public static class CoreServicesExtensions
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.TryAddSingleton<IApplicationContext, GenericApplicationContext>();
            services.TryAddSingleton<IConversionService>(DefaultConversionService.Singleton);
            services.TryAddSingleton<ILifecycleProcessor, DefaultLifecycleProcessor>();

            return services;
        }
    }
}
