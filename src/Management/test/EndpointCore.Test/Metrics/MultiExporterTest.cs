// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using OpenTelemetry.Trace;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.OpenTelemetry.Metrics;
using Steeltoe.Management.OpenTelemetry.Metrics.Export;
using Steeltoe.Management.OpenTelemetry.Metrics.Exporter;
using Steeltoe.Management.OpenTelemetry.Metrics.Factory;
using Steeltoe.Management.OpenTelemetry.Metrics.Processor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Metrics
{
    [Obsolete]
    public class MultiExporterTest
    {
        [Fact]
        public async Task MultipleExportersTestAsync()
        {
            var opts = new PrometheusEndpointOptions();
            var mopts = new ActuatorManagementOptions();
            mopts.EndpointOptions.Add(opts);
            var exporter1 = new PrometheusExporter();
            var exporter2 = new SteeltoeExporter();
            var multiExporter = new MultiExporter(new MetricExporter[] { exporter1, exporter2 }.ToList());
            var processor = new SteeltoeProcessor(multiExporter);

            var factory = AutoCollectingMeterFactory.Create(processor);
            var meter = factory.GetMeter("Test");
            SetupTestView(meter);
            factory.CollectAllMetrics();
            processor.ExportMetrics();

            Task.Delay(1000).Wait();

            var ep = new PrometheusScraperEndpoint(opts, exporter1);
            var middle = new PrometheusScraperEndpointMiddleware(null, ep, mopts);

            var context = CreateRequest("GET", "/actuator/prometheus");

            await middle.HandleMetricsRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var rdr = new StreamReader(context.Response.Body);
            var text = await rdr.ReadToEndAsync();
            Assert.Equal("# HELP test Testtest\n# TYPE test counter\ntest{a=\"v1\",b=\"v1\",c=\"v1\"} 45\n", text);

            var meopts = new MetricsEndpointOptions();
            var ep2 = new MetricsEndpoint(meopts, exporter2);
            var middle2 = new MetricsEndpointMiddleware(null, ep2, mopts);

            var context2 = CreateRequest("GET", "/actuator/metrics/test", "?tag=a:v1");

            await middle2.HandleMetricsRequestAsync(context2);
            Assert.Equal(200, context.Response.StatusCode);

            context2.Response.Body.Seek(0, SeekOrigin.Begin);
            var rdr2 = new StreamReader(context2.Response.Body);
            var json = await rdr2.ReadToEndAsync();
            Assert.Equal("{\"name\":\"test\",\"measurements\":[{\"statistic\":\"COUNT\",\"value\":45}],\"availableTags\":[{\"tag\":\"a\",\"values\":[\"v1\"]},{\"tag\":\"b\",\"values\":[\"v1\"]},{\"tag\":\"c\",\"values\":[\"v1\"]}]}", json);
        }

        private HttpContext CreateRequest(string method, string path, string query = null)
        {
            HttpContext context = new DefaultHttpContext
            {
                TraceIdentifier = Guid.NewGuid().ToString()
            };
            context.Response.Body = new MemoryStream();
            context.Request.Method = method;
            context.Request.Path = new PathString(path);
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("localhost");
            if (!string.IsNullOrEmpty(query))
            {
                context.Request.QueryString = new QueryString(query);
            }

            return context;
        }

        private void SetupTestView(Meter meter)
        {
            var measure = meter.CreateDoubleCounter("test");
            var labels = new Dictionary<string, string>()
            {
                { "a", "v1" },
                { "b", "v1" },
                { "c", "v1" }
            }.ToList();

            for (var i = 0; i < 10; i++)
            {
                measure.Add(default(SpanContext), i, labels);
            }
        }
    }
}
