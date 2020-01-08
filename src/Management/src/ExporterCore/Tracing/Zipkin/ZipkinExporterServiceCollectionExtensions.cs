// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OpenCensus.Exporter.Zipkin;
using Steeltoe.Common;
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
            var opts = new TraceExporterOptions(p.GetApplicationInstanceInfo(), config);
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
