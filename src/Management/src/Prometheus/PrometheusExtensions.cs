using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using Steeltoe.Common;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Metrics.Prometheus;
using Steeltoe.Management.MetricCollectors;

namespace Steeltoe.Management.Prometheus;
public static class PrometheusExtensions
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
    public static IServiceCollection AddPrometheusActuator(this IServiceCollection services)
    {
        ArgumentGuard.NotNull(services);

        services.AddOpenTelemetry()

        .WithMetrics(builder =>
        {
            builder.AddMeter(SteeltoeMetrics.InstrumentationName);
            var idpdb = builder as IDeferredMeterProviderBuilder;
            idpdb?.Configure((provider, builder) =>
            {
                //provider.GetRequiredService<IManagementOptions>


                //provider.GetRequiredService< PrometheusEndpointOptions>
                var mgmtOptions = new ActuatorManagementOptions();
                var prometheusOptions = new PrometheusEndpointOptions();
                //builder.ConfigureServices(services => services.Configure<PrometheusAspNetCoreOptions>(options =>
                //{
                //    options.ScrapeResponseCacheDurationMilliseconds = (int)prometheusOptions.ScrapeResponseCacheDurationMilliseconds;
                //    options.ScrapeEndpointPath = mgmtOptions.Path + "/" + prometheusOptions.Id;
                //}));

            });

            builder.AddPrometheusExporter();
        })
        .StartWithHost();

        return services;
    }
    public static IApplicationBuilder MapPrometheusActuator(
          this IApplicationBuilder app)
    {
        // Note: Order is important here. MeterProvider is accessed before
        // GetOptions<PrometheusExporterOptions> so that any changes made to
        // PrometheusExporterOptions in deferred AddPrometheusExporter
        // configure actions are reflected.
        var meterProvider = app.ApplicationServices.GetRequiredService<MeterProvider>();
        var managementOptions = app.ApplicationServices.GetService<IEnumerable<IManagementOptions>>()?.OfType<ActuatorManagementOptions>().FirstOrDefault();
        // var prometheusOptions = app.ApplicationServices.GetServices<>  TODO configure
        var prometheusOptions = new PrometheusEndpointOptions(); //Default


        var path = managementOptions.Path + "/" + prometheusOptions.Id;

        //if (!path.StartsWith("/"))
        //{
        //    path = $"/{path}";
        //}

        //return app.Map(
        //    new PathString(path),
        //    builder =>
        //    {
        //       // configureBranchedPipeline?.Invoke(builder);
        //        builder.UseMiddleware<PrometheusExporterMiddleware>(meterProvider);
        //    });

        return app.UseOpenTelemetryPrometheusScrapingEndpoint(path);
    }
}
