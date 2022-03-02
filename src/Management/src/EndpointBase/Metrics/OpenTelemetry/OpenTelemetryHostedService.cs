using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Metrics.OpenTelemetry
{
    internal class TelemetryHostedService : IHostedService
    {
        private readonly IServiceProvider serviceProvider;

        public TelemetryHostedService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                // The sole purpose of this HostedService is to ensure all
                // instrumentations, exporters, etc., are created and started.
                var meterProvider = this.serviceProvider.GetService<MeterProvider>();
                var tracerProvider = this.serviceProvider.GetService<TracerProvider>();

                if (meterProvider == null && tracerProvider == null)
                {
                    throw new InvalidOperationException("Could not resolve either MeterProvider or TracerProvider through application ServiceProvider, OpenTelemetry SDK has not been initialized.");
                }
            }
            catch (Exception ex)
            {
              //  HostingExtensionsEventSource.Log.FailedOpenTelemetrySDK(ex);
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
