// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Steeltoe.Common.Contexts;
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
            services.TryAddSingleton<IApplicationContext, GenericApplicationContext>();
            services.TryAddSingleton<IDestinationRegistry, DefaultDestinationRegistry>();
            services.TryAddSingleton<IConversionService>(DefaultConversionService.Singleton);
            services.TryAddSingleton<ILifecycleProcessor, DefaultLifecycleProcessor>();

            services.TryAddSingleton<IExpressionParser, ExpressionParser>();
            services.TryAddSingleton<IEvaluationContext, EvaluationContext>();

            return services;
        }
    }
}
