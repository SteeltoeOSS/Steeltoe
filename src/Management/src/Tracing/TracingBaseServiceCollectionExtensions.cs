// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.Http;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Steeltoe.Common;
using Steeltoe.Common.Reflection;
using Steeltoe.Logging;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Wavefront.Exporters;
using B3Propagator = OpenTelemetry.Extensions.Propagators.B3Propagator;
using Sdk = OpenTelemetry.Sdk;

namespace Steeltoe.Management.Tracing;

public static class TracingBaseServiceCollectionExtensions
{
    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient Instrumentation.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection" />.
    /// </param>
    /// <returns>
    /// <see cref="IServiceCollection" /> configured for distributed tracing.
    /// </returns>
    public static IServiceCollection AddDistributedTracing(this IServiceCollection services)
    {
        return services.AddDistributedTracing(null);
    }

    /// <summary>
    /// Configure distributed tracing via OpenTelemetry with HttpClient Instrumentation.
    /// </summary>
    /// <param name="services">
    /// <see cref="IServiceCollection" />.
    /// </param>
    /// <param name="action">
    /// Customize the <see cref="TracerProviderBuilder" />.
    /// </param>
    /// <returns>
    /// <see cref="IServiceCollection" /> configured for distributed tracing.
    /// </returns>
    public static IServiceCollection AddDistributedTracing(this IServiceCollection services, Action<TracerProviderBuilder>? action)
    {
        ArgumentGuard.NotNull(services);

        services.AddOptions();
        services.RegisterDefaultApplicationInstanceInfo();

        services.TryAddSingleton<ITracingOptions>(serviceProvider =>
            new TracingOptions(serviceProvider.GetRequiredService<IApplicationInstanceInfo>(), serviceProvider.GetRequiredService<IConfiguration>()));

        services.ConfigureOptionsWithChangeTokenSource<WavefrontExporterOptions, ConfigureWavefrontExporterOptions>();

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IDynamicMessageProcessor, TracingLogProcessor>());

        bool exportToZipkin = ReflectionHelpers.IsAssemblyLoaded("OpenTelemetry.Exporter.Zipkin");
        bool exportToJaeger = ReflectionHelpers.IsAssemblyLoaded("OpenTelemetry.Exporter.Jaeger");
        bool exportToOpenTelemetryProtocol = ReflectionHelpers.IsAssemblyLoaded("OpenTelemetry.Exporter.OpenTelemetryProtocol");

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

        services.AddOpenTelemetry().WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder.AddHttpClientInstrumentation();

            if (exportToZipkin)
            {
                AddZipkinExporter(tracerProviderBuilder);
            }

            if (exportToJaeger)
            {
                AddJaegerExporter(tracerProviderBuilder);
            }

            if (exportToOpenTelemetryProtocol)
            {
                AddOpenTelemetryProtocolExporter(tracerProviderBuilder);
            }

            action?.Invoke(tracerProviderBuilder);
        });

        services.AddOptions<HttpClientTraceInstrumentationOptions>().Configure<IServiceProvider>((options, serviceProvider) =>
        {
            var tracingOptions = serviceProvider.GetRequiredService<ITracingOptions>();

            var pathMatcher = new Regex(tracingOptions.EgressIgnorePattern);
            options.FilterHttpRequestMessage += requestMessage => !pathMatcher.IsMatch(requestMessage.RequestUri?.PathAndQuery ?? string.Empty);
        });

        services.ConfigureOpenTelemetryTracerProvider((serviceProvider, tracerProviderBuilder) =>
        {
            string appName = serviceProvider.GetRequiredService<IApplicationInstanceInfo>()
                .GetApplicationNameInContext(SteeltoeComponent.Management, $"{TracingOptions.ConfigurationPrefix}:name");

            var tracingOptions = serviceProvider.GetRequiredService<ITracingOptions>();

            ILogger logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger($"{typeof(TracingBaseServiceCollectionExtensions).Namespace}.Setup");

            logger.LogTrace("Found Zipkin exporter: {exportToZipkin}. Found Jaeger exporter: {exportToJaeger}. Found OTLP exporter: {exportToOtlp}.",
                exportToZipkin, exportToJaeger, exportToOpenTelemetryProtocol);

            tracerProviderBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(appName));

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
                tracerProviderBuilder.SetSampler(new AlwaysOffSampler());
            }
            else if (tracingOptions.AlwaysSample)
            {
                tracerProviderBuilder.SetSampler(new AlwaysOnSampler());
            }

            AddWavefrontExporter(tracerProviderBuilder, serviceProvider);
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

    private static void AddZipkinExporter(TracerProviderBuilder builder)
    {
        builder.AddZipkinExporter();
    }

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

    private static void AddJaegerExporter(TracerProviderBuilder builder)
    {
        builder.AddJaegerExporter();
    }

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

    private static void AddOpenTelemetryProtocolExporter(TracerProviderBuilder builder)
    {
        builder.AddOtlpExporter();
    }

    private static void AddWavefrontExporter(TracerProviderBuilder tracerProviderBuilder, IServiceProvider serviceProvider)
    {
        var wavefrontOptions = serviceProvider.GetRequiredService<IOptions<WavefrontExporterOptions>>();

        // Only add if wavefront is configured
        if (!string.IsNullOrEmpty(wavefrontOptions.Value.Uri))
        {
            var logger = serviceProvider.GetRequiredService<ILogger<WavefrontTraceExporter>>();
            tracerProviderBuilder.AddWavefrontTraceExporter(wavefrontOptions.Value, logger);
        }
    }
}
