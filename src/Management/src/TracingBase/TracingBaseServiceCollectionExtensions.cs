// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.Tracing
{
    public static class TracingBaseServiceCollectionExtensions
    {
        /// <summary>
        /// Configure distributed tracing via OpenTelemetry with HttpClient Instrumentation.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection" /></param>
        /// <returns><see cref="IServiceCollection"/> configured for distributed tracing.</returns>
        public static IServiceCollection AddDistributedTracing(this IServiceCollection services) => services.AddDistributedTracing(null);

        /// <summary>
        /// Configure distributed tracing via OpenTelemetry with HttpClient Instrumentation.
        /// </summary>
        /// <param name="services"><see cref="IServiceCollection" /></param>
        /// <param name="action">Customize the <see cref="TracerProviderBuilder" />.</param>
        /// <returns><see cref="IServiceCollection"/> configured for distributed tracing.</returns>
        public static IServiceCollection AddDistributedTracing(this IServiceCollection services, Action<TracerProviderBuilder> action)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();
            services.RegisterDefaultApplicationInstanceInfo();
            services.TryAddSingleton<ITracingOptions>((serviceProvider) => new TracingOptions(serviceProvider.GetRequiredService<IApplicationInstanceInfo>(), serviceProvider.GetRequiredService<IConfiguration>()));
            services.TryAddSingleton<IDynamicMessageProcessor, TracingLogProcessor>();

            services.AddOpenTelemetryTracing(builder =>
            {
                builder.Configure((serviceProvider, deferredBuilder) =>
                {
                    var appName = serviceProvider.GetRequiredService<IApplicationInstanceInfo>().ApplicationNameInContext(SteeltoeComponent.Management, TracingOptions.CONFIG_PREFIX + ":name");
                    var traceOpts = serviceProvider.GetRequiredService<ITracingOptions>();
                    deferredBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(appName));
                    deferredBuilder.AddHttpClientInstrumentation(options =>
                        {
                            var pathMatcher = new Regex(traceOpts.EgressIgnorePattern);
                            options.Filter += req => !pathMatcher.IsMatch(req.RequestUri.PathAndQuery);
                        });

                    if (traceOpts.PropagationType.Equals("B3", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var propagators = new List<TextMapPropagator> { new B3Propagator(traceOpts.SingleB3Header), new BaggagePropagator() };
                        Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(propagators));
                    }

                    // To Discuss: Should these remain Steeltoe options or downstream set directly?
                    if (traceOpts.NeverSample)
                    {
                        deferredBuilder.SetSampler<AlwaysOffSampler>();
                    }
                    else if (traceOpts.AlwaysSample)
                    {
                        deferredBuilder.SetSampler<AlwaysOnSampler>();
                    }
                });

                action?.Invoke(builder);
            });

            return services;
        }
    }
}
