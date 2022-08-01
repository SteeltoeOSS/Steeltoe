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
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Common.Reflection;
using Steeltoe.Extensions.Logging;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Exporters.Wavefront;
using Steeltoe.Management.OpenTelemetry.Trace;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.Tracing;

public static class TracingBaseServiceCollectionExtensions
{
    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient Instrumentation.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection" />.</param>
    /// <returns><see cref="IServiceCollection"/> configured for distributed tracing.</returns>
    public static IServiceCollection AddDistributedTracing(this IServiceCollection services) => services.AddDistributedTracing(null);

    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient Instrumentation.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection" />.</param>
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
        services.TryAddSingleton<ITracingOptions>(serviceProvider => new TracingOptions(serviceProvider.GetRequiredService<IApplicationInstanceInfo>(), serviceProvider.GetRequiredService<IConfiguration>()));
        services.TryAddSingleton<IDynamicMessageProcessor, TracingLogProcessor>();

        var exportToZipkin = ReflectionHelpers.IsAssemblyLoaded("OpenTelemetry.Exporter.Zipkin");
        var exportToJaeger = ReflectionHelpers.IsAssemblyLoaded("OpenTelemetry.Exporter.Jaeger");
        var exportToOpenTelemetryProtocol = ReflectionHelpers.IsAssemblyLoaded("OpenTelemetry.Exporter.OpenTelemetryProtocol");

        if (exportToZipkin)
        {
            ConfigureZipkinOptions(services);
        }

        if (exportToJaeger)
        {
            ConfigureJaegerOptions(services);
        }

        if (exportToOpenTelemetryProtocol)
        {
            ConfigureOpenTelemetryProtocolOptions(services);
        }

        services.AddOpenTelemetryTracing(builder =>
        {
            builder.Configure((serviceProvider, deferredBuilder) =>
            {
                var appName = serviceProvider.GetRequiredService<IApplicationInstanceInfo>().ApplicationNameInContext(SteeltoeComponent.Management, $"{TracingOptions.ConfigPrefix}:name");
                var traceOpts = serviceProvider.GetRequiredService<ITracingOptions>();
                var logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger("Steeltoe.Management.Tracing.Setup");
                logger?.LogTrace("Found Zipkin exporter: {exportToZipkin}. Found Jaeger exporter: {exportToJaeger}. Found OTLP exporter: {exportToOtlp}.", exportToZipkin, exportToJaeger, exportToOpenTelemetryProtocol);
                deferredBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(appName));
                deferredBuilder.AddHttpClientInstrumentation(options =>
                {
                    var pathMatcher = new Regex(traceOpts.EgressIgnorePattern);
                    options.Filter += req => !pathMatcher.IsMatch(req.RequestUri.PathAndQuery);
                });

                if (traceOpts.PropagationType.Equals("B3", StringComparison.InvariantCultureIgnoreCase))
                {
                    // TODO: Investigate alternatives and remove suppression.
#pragma warning disable CS0618 // Type or member is obsolete
                    var propagators = new List<TextMapPropagator> { new B3Propagator(traceOpts.SingleB3Header), new BaggagePropagator() };
#pragma warning restore CS0618 // Type or member is obsolete
                    Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(propagators));
                }

                if (traceOpts.NeverSample)
                {
                    deferredBuilder.SetSampler(new AlwaysOffSampler());
                }
                else if (traceOpts.AlwaysSample)
                {
                    deferredBuilder.SetSampler(new AlwaysOnSampler());
                }
            });

            if (exportToZipkin)
            {
                AddZipkinExporter(builder);
            }

            if (exportToJaeger)
            {
                AddJaegerExporter(builder);
            }

            if (exportToOpenTelemetryProtocol)
            {
                AddOpenTelemetryProtocolExporter(builder);
            }

            AddWavefrontExporter(builder);

            action?.Invoke(builder);
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

    private static void ConfigureJaegerOptions(IServiceCollection services)
    {
        services.AddOptions<JaegerExporterOptions>().PostConfigure<ITracingOptions>((options, traceOpts) =>
        {
            options.MaxPayloadSizeInBytes = traceOpts.MaxPayloadSizeInBytes;

            if (traceOpts.ExporterEndpoint != null)
            {
                options.AgentHost = traceOpts.ExporterEndpoint.Host;
                options.AgentPort = traceOpts.ExporterEndpoint.Port;
            }
        });
    }

    private static void AddJaegerExporter(TracerProviderBuilder builder) => builder.AddJaegerExporter();

    private static void ConfigureOpenTelemetryProtocolOptions(IServiceCollection services)
    {
        services.AddOptions<OtlpExporterOptions>().PostConfigure<ITracingOptions>((options, traceOpts) =>
        {
            if (traceOpts.ExporterEndpoint != null)
            {
                options.Endpoint = traceOpts.ExporterEndpoint;
            }
        });
    }

    private static void AddOpenTelemetryProtocolExporter(TracerProviderBuilder builder) => builder.AddOtlpExporter();

    private static void AddWavefrontExporter(TracerProviderBuilder builder)
    {
        var deferredTracerProviderBuilder = builder as IDeferredTracerProviderBuilder;
        deferredTracerProviderBuilder.Configure(delegate(IServiceProvider sp, TracerProviderBuilder builder)
        {
            var config = sp.GetService<IConfiguration>();
            var wavefrontOptions = new WavefrontExporterOptions(config);

            // Only add if wavefront is configured
            if (!string.IsNullOrEmpty(wavefrontOptions.Uri))
            {
                var logger = sp.GetService<ILogger<WavefrontTraceExporter>>();
                builder.AddWavefrontExporter(wavefrontOptions, logger);
            }
        });
    }
}
