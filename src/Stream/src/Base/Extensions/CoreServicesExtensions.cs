// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Converter;
using Steeltoe.Common.Expression;
using Steeltoe.Common.Expression.CSharp;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.Core;

namespace Steeltoe.Stream.Extensions
{
    public static class CoreServicesExtensions
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.TryAddSingleton<IDestinationRegistry, DefaultDestinationRegistry>();
            services.TryAddSingleton<IConversionService>(DefaultConversionService.Singleton);
            services.TryAddSingleton<ILifecycleProcessor, DefaultLifecycleProcessor>();

            services.TryAddSingleton<IExpressionParser, ExpressionParser>();
            services.TryAddSingleton<IEvaluationContext, EvaluationContext>();

            return services;
        }
    }
}
