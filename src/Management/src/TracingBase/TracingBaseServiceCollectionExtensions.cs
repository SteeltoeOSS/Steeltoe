// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Common.Reflection;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Exporters.Wavefront;
using Steeltoe.Management.OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using B3Propagator = OpenTelemetry.Extensions.Propagators.B3Propagator;
using Sdk = OpenTelemetry.Sdk;

namespace Steeltoe.Management.Tracing;

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

        services.AddLogging();
        services.AddOptions();
        services.RegisterDefaultApplicationInstanceInfo();
        services.TryAddSingleton<ITracingOptions>(serviceProvider => new TracingOptions(serviceProvider.GetRequiredService<IApplicationInstanceInfo>(), serviceProvider.GetRequiredService<IConfiguration>()));
        services.TryAddSingleton<IDynamicMessageProcessor, TracingLogProcessor>();

        var exportToZipkin = ReflectionHelpers.IsAssemblyLoaded("OpenTelemetry.Exporter.Zipkin");
        var exportToOtlp = ReflectionHelpers.IsAssemblyLoaded("OpenTelemetry.Exporter.OpenTelemetryProtocol");

        if (exportToZipkin)
        {
            ConfigureZipkinOptions(services);
        }

        if (exportToOtlp)
        {
            ConfigureOtlpOptions(services);
        }

        services.AddOpenTelemetry().WithTracing(builder =>
        {
            builder.AddHttpClientInstrumentation();

            if (exportToZipkin)
            {
                AddZipkinExporter(builder);
            }

            if (exportToOtlp)
            {
                AddOtlpExporter(builder);
            }

            action?.Invoke(builder);
        });

        services.AddOptions<HttpClientTraceInstrumentationOptions>().Configure<IServiceProvider>((options, serviceProvider) =>
        {
            var tracingOptions = serviceProvider.GetRequiredService<ITracingOptions>();

            var pathMatcher = new Regex(tracingOptions.EgressIgnorePattern);
            options.FilterHttpRequestMessage += requestMessage => !pathMatcher.IsMatch(requestMessage.RequestUri?.PathAndQuery ?? string.Empty);
        });

        services.ConfigureOpenTelemetryTracerProvider((serviceProvider, builder) =>
        {
            string appName = serviceProvider.GetRequiredService<IApplicationInstanceInfo>()
                .ApplicationNameInContext(SteeltoeComponent.Management, $"{TracingOptions.CONFIG_PREFIX}:name");

            var tracingOptions = serviceProvider.GetRequiredService<ITracingOptions>();

            ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger($"{typeof(TracingBaseServiceCollectionExtensions).Namespace}.Setup");

            logger.LogTrace("Found Zipkin exporter: {exportToZipkin}. Found OTLP exporter: {exportToOtlp}.", exportToZipkin, exportToOtlp);

            builder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(appName));

            if (tracingOptions.PropagationType.Equals("B3", StringComparison.OrdinalIgnoreCase))
            {
                var propagators = new List<TextMapPropagator>
                {
                    new B3Propagator(tracingOptions.SingleB3Header),
                    new BaggagePropagator()
                };

                Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(propagators));
            }

            if (tracingOptions.NeverSample)
            {
                builder.SetSampler(new AlwaysOffSampler());
            }
            else if (tracingOptions.AlwaysSample)
            {
                builder.SetSampler(new AlwaysOnSampler());
            }

            AddWavefrontExporter(builder, serviceProvider);
        });

        return services;
    }

    private static void ConfigureZipkinOptions(IServiceCollection services)
    {
        services.AddOptions<ZipkinExporterOptions>().PostConfigure<ITracingOptions>((options, traceOpts) =>
        {
            options.UseShortTraceIds = traceOpts.UseShortTraceIds;
            options.MaxPayloadSizeInBytes = traceOpts.MaxPayloadSizeInBytes;

            if (traceOpts.ExporterEndpoint != null)
            {
                options.Endpoint = traceOpts.ExporterEndpoint;
            }
        });
    }

    private static void AddZipkinExporter(TracerProviderBuilder builder) => builder.AddZipkinExporter();

    private static void ConfigureOtlpOptions(IServiceCollection services)
    {
        services.AddOptions<OtlpExporterOptions>().PostConfigure<ITracingOptions>((options, traceOpts) =>
        {
            if (traceOpts.ExporterEndpoint != null)
            {
                options.Endpoint = traceOpts.ExporterEndpoint;
            }
        });
    }

    private static void AddOtlpExporter(TracerProviderBuilder builder) => builder.AddOtlpExporter();

    private static void AddWavefrontExporter(TracerProviderBuilder builder, IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var wavefrontOptions = new WavefrontExporterOptions(configuration);

        // Only add if wavefront is configured
        if (!string.IsNullOrEmpty(wavefrontOptions.Uri))
        {
            var logger = serviceProvider.GetRequiredService<ILogger<WavefrontTraceExporter>>();
            var exporter = new WavefrontTraceExporter(wavefrontOptions, logger);
            builder.AddProcessor(new BatchActivityExportProcessor(exporter, wavefrontOptions.MaxQueueSize, wavefrontOptions.Step));
        }
    }
}
