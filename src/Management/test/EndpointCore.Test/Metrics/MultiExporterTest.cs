﻿// Copyright 2017 the original author or authors.
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

using Microsoft.AspNetCore.Http;
using OpenTelemetry.Exporter.Prometheus;
using OpenTelemetry.Metrics;
using OpenTelemetry.Metrics.Export;
using OpenTelemetry.Trace;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.OpenTelemetry.Metrics.Exporter;
using Steeltoe.Management.OpenTelemetry.Metrics.Factory;
using Steeltoe.Management.OpenTelemetry.Metrics.Processor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using PrometheusExporter = Steeltoe.Management.OpenTelemetry.Metrics.Exporter.PrometheusExporter;

namespace Steeltoe.Management.EndpointCore.Test.Metrics
{
    public class MultiExporterTest
    {
        [Fact]
        public async Task MultipleExportersTestAsync()
        {
            var opts = new PrometheusEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
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

            var context2 = CreateRequest("GET", "/cloudfoundryapplication/metrics/test", "?tag=a:v1");

            await middle2.HandleMetricsRequestAsync(context2);
            Assert.Equal(200, context.Response.StatusCode);

            context2.Response.Body.Seek(0, SeekOrigin.Begin);
            StreamReader rdr2 = new StreamReader(context2.Response.Body);
            string json = await rdr2.ReadToEndAsync();
            Assert.Equal("{\"name\":\"test\",\"measurements\":[{\"statistic\":\"COUNT\",\"value\":45.0}],\"availableTags\":[{\"tag\":\"a\",\"values\":[\"v1\"]},{\"tag\":\"b\",\"values\":[\"v1\"]},{\"tag\":\"c\",\"values\":[\"v1\"]}]}", json);
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

            for (int i = 0; i < 10; i++)
            {
                measure.Add(default(SpanContext), i, labels);
            }
        }
    }
}
