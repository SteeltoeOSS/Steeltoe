namespace Steeltoe.Management.Wavefront;
public class Class1
{

    //TODO: Move to separate library
    /// <summary>
    /// Adds the services used by the Wavefront exporter.
    /// </summary>
    /// <param name="services">
    /// Reference to the service collection.
    /// </param>
    /// <returns>
    /// A reference to the service collection.
    /// </returns>
    //public static IServiceCollection AddWavefrontMetrics(this IServiceCollection services)
    //{
    //    ArgumentGuard.NotNull(services);

    //    services.TryAddSingleton<IDiagnosticsManager, DiagnosticsManager>();
    //    services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DiagnosticServices>());

    //    services.TryAddSingleton<IMetricsObserverOptions>(provider =>
    //    {
    //        var configuration = provider.GetService<IConfiguration>();
    //        return new MetricsObserverOptions(configuration);
    //    });

    //    services.TryAddSingleton<IViewRegistry, ViewRegistry>();

    //    AddMetricsObservers(services);

    //    services.TryAddSingleton(provider =>
    //    {
    //        var logger = provider.GetService<ILogger<WavefrontMetricsExporter>>();
    //        var configuration = provider.GetService<IConfiguration>();
    //        return new WavefrontMetricsExporter(new WavefrontExporterOptions(configuration), logger);
    //    });

    //    services.AddOpenTelemetryMetricsForSteeltoe();

    //    return services;
    //}

    //[Fact]
    //public async Task AddWavefrontExporter()
    //{
    //    var settings = new Dictionary<string, string>
    //    {
    //        { "management:metrics:export:wavefront:apiToken", "test" },
    //        { "management:metrics:export:wavefront:uri", "http://test.io" },
    //        { "management:metrics:export:wavefront:step", "500" }
    //    };

    //    WebApplicationBuilder builder = WebApplication.CreateBuilder();
    //    builder.Configuration.AddInMemoryCollection(settings);
    //    builder.WebHost.UseTestServer();

    //    WebApplication host = builder.AddWavefrontMetrics().Build();

    //    await host.StartAsync();

    //    await Task.Delay(3000);

    //    // Exercise the deferred builder logic by starting the test host.
    //    // Validate the exporter got actually added
    //    var exporter = host.Services.GetService<WavefrontMetricsExporter>();
    //    Assert.NotNull(exporter);
    //    await host.StopAsync();
    //}

    //[Fact]
    //public void AddWavefront_IWebHostBuilder()
    //{
    //    var wfSettings = new Dictionary<string, string>
    //    {
    //        { "management:metrics:export:wavefront:uri", "https://wavefront.vmware.com" },
    //        { "management:metrics:export:wavefront:apiToken", "testToken" }
    //    };

    //    IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
    //    {
    //    }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(wfSettings));

    //    IWebHost host = hostBuilder.AddWavefrontMetrics().Build();

    //    IEnumerable<IDiagnosticsManager> diagnosticsManagers = host.Services.GetServices<IDiagnosticsManager>();
    //    Assert.Single(diagnosticsManagers);
    //    IEnumerable<DiagnosticServices> diagnosticServices = host.Services.GetServices<IHostedService>().OfType<DiagnosticServices>();
    //    Assert.Single(diagnosticServices);
    //    IEnumerable<IMetricsObserverOptions> options = host.Services.GetServices<IMetricsObserverOptions>();
    //    Assert.Single(options);
    //    IEnumerable<IViewRegistry> viewRegistry = host.Services.GetServices<IViewRegistry>();
    //    Assert.Single(viewRegistry);
    //    IEnumerable<WavefrontMetricsExporter> exporters = host.Services.GetServices<WavefrontMetricsExporter>();
    //    Assert.Single(exporters);
    //}

    //[Fact]
    //public void AddWavefront_ProxyConfigIsValid()
    //{
    //    var wfSettings = new Dictionary<string, string>
    //    {
    //        { "management:metrics:export:wavefront:uri", "proxy://wavefront.vmware.com" },
    //        { "management:metrics:export:wavefront:apiToken", string.Empty } // Should not throw
    //    };

    //    IWebHostBuilder hostBuilder = new WebHostBuilder().Configure(_ =>
    //    {
    //    }).ConfigureAppConfiguration(builder => builder.AddInMemoryCollection(wfSettings));

    //    IWebHost host = hostBuilder.AddWavefrontMetrics().Build();

    //    IEnumerable<WavefrontMetricsExporter> exporters = host.Services.GetServices<WavefrontMetricsExporter>();
    //    Assert.Single(exporters);
    //}


}
