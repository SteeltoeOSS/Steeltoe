namespace Steeltoe.Management.Prometheus;
public class Class1
{


    /// <summary>
    /// Adds the services used by the Prometheus actuator.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <param name="configuration">
    /// Reference to the configuration system.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    /// TODO: Move to separate assembly
    //public static IServiceCollection AddPrometheusActuatorServices(this IServiceCollection services, IConfiguration configuration)
    //{
    //    ArgumentGuard.NotNull(services);
    //    ArgumentGuard.NotNull(configuration);

    //    var options = new PrometheusEndpointOptions(configuration);
    //    services.TryAddSingleton<IPrometheusEndpointOptions>(options);
    //    services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IEndpointOptions), options));
    //    services.TryAddSingleton<PrometheusScraperEndpoint>();

    //    services.TryAddEnumerable(ServiceDescriptor.Singleton<MetricsExporter, SteeltoePrometheusExporter>(provider =>
    //    {
    //        var options = provider.GetService<IMetricsEndpointOptions>();

    //        var exporterOptions = new PullMetricsExporterOptions
    //        {
    //            ScrapeResponseCacheDurationMilliseconds = options.ScrapeResponseCacheDurationMilliseconds
    //        };

    //        return new SteeltoePrometheusExporter(exporterOptions);
    //    }));

    //    services.AddOpenTelemetryMetricsForSteeltoe();

    //    return services;
    //}
    //TODO: Move to separate assembly
    //public static void AddPrometheusActuator(this IServiceCollection services, IConfiguration configuration = null)
    //{
    //    ArgumentGuard.NotNull(services);

    //    configuration ??= services.BuildServiceProvider().GetRequiredService<IConfiguration>();

    //    services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
    //    services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DiagnosticServices>());

    //    services.TryAddEnumerable(ServiceDescriptor.Singleton<IManagementOptions>(new ActuatorManagementOptions(configuration)));

    //    var metricsEndpointOptions = new MetricsEndpointOptions(configuration);
    //    services.TryAddSingleton<IMetricsEndpointOptions>(metricsEndpointOptions);

    //    var observerOptions = new MetricsObserverOptions(configuration);
    //    services.TryAddSingleton<IMetricsObserverOptions>(observerOptions);
    //    // services.TryAddSingleton<IViewRegistry, ViewRegistry>();
    //    services.TryAddSingleton<PrometheusEndpointOptions>();

    //    services.AddPrometheusActuatorServices(configuration);

    //    AddMetricsObservers(services);

    //    services.AddActuatorEndpointMapping<PrometheusScraperEndpoint>();
    //}

}
