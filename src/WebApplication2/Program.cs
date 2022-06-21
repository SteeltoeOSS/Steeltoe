using Steeltoe.Management.Endpoint;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Steeltoe.Management.Tracing;
using Steeltoe.Bootstrap.Autoconfig;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using System.Diagnostics.Metrics;
using OpenTelemetry.Exporter;

var builder = WebApplication.CreateBuilder(args); 
   builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//builder.Services.AddDistributedTracing();
//builder.Services.AddDistributedTracingAspNetCore();
//builder.AddCloudFoundryConfiguration();

//builder.Services.ActivateActuatorEndpoints();


// builder.AddSteeltoe();



//builder.Services.AddOpenTelemetryMetrics(b =>
//   {
//       //b.ConfigureSteeltoeMetrics();
//       b.AddMeter("test")
//       //.Build();
//           .AddConsoleExporter((exporterOptions, metricReaderOptions) =>
//           {
//               exporterOptions.Targets = ConsoleExporterOutputTargets.Console;

//               metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 2000;
//               metricReaderOptions.TemporalityPreference = MetricReaderTemporalityPreference.Cumulative;

//           });

//       //b.Configure((provider, deferredBuilder) =>
//       //{
//       //    deferredBuilder.AddMeter("test")
//       //    .AddConsoleExporter((exporterOptions, metricReaderOptions) =>
//       //    {
//       //        exporterOptions.Targets = ConsoleExporterOutputTargets.Console;

//       //        metricReaderOptions.PeriodicExportingMetricReaderOptions.ExportIntervalMilliseconds = 2000;
//       //        metricReaderOptions.TemporalityPreference = MetricReaderTemporalityPreference.Cumulative;

//       //    });


//       //});
//   });
builder.AddWavefrontMetrics();
builder.AddAllActuators();
foreach (var service in builder.Services)
{
    if (service.GetType() == typeof(MeterProviderBuilder))
    {
        Console.WriteLine(service.ToString());
    }
}

var app = builder.Build();
var meter = new Meter("test");
var testCounter = meter.CreateCounter<int>("testCounter");
var timercb = new TimerCallback((t) =>
{
    for (int i = 0; i < 100; i++)
    {
        testCounter.Add(1);


    }
});
var timer = new Timer(timercb,null,0,1000);
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
