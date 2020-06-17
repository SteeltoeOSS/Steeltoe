// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpenCensus.Exporter.Zipkin;
using Steeltoe.Management.Census.Trace;
using Steeltoe.Management.Exporter.Tracing.Zipkin;
using System;
using System.Net.Http;

namespace Steeltoe.Management.Exporter.Tracing
{
    public static class ZipkinExporterServiceCollectionExtensions
    {
        public static void AddZipkinExporter(this IServiceCollection services, IConfiguration config)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            services.TryAddSingleton((p) =>
            {
                return CreateExporter(p, config);
            });
        }

        private static ZipkinTraceExporter CreateExporter(IServiceProvider p, IConfiguration config)
        {
#if NETCOREAPP3_1
            var h = p.GetRequiredService<IHostEnvironment>();
#else
            var h = p.GetRequiredService<IHostingEnvironment>();
#endif
            var opts = new TraceExporterOptions(h.ApplicationName, config);
            var censusOpts = new ZipkinTraceExporterOptions()
            {
                Endpoint = new Uri(opts.Endpoint),
                TimeoutSeconds = TimeSpan.FromSeconds(opts.TimeoutSeconds),
                ServiceName = opts.ServiceName,
                UseShortTraceIds = opts.UseShortTraceIds
            };

            var tracing = p.GetRequiredService<ITracing>();
            if (!opts.ValidateCertificates)
            {
                var client = new HttpClient(
                    new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = (mesg, cert, chain, errors) => { return true; }
                    });
                return new ZipkinTraceExporter(censusOpts, tracing.ExportComponent, client);
            }
            else
            {
                return new ZipkinTraceExporter(censusOpts, tracing.ExportComponent);
            }
        }
    }
}
