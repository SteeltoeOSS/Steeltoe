﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.OpenTelemetry.Metrics.Factory;
using Steeltoe.Management.OpenTelemetry.Metrics.Processor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using PrometheusExporter = Steeltoe.Management.OpenTelemetry.Metrics.Exporter.PrometheusExporter;

namespace Steeltoe.Management.Endpoint.Metrics.Test
{
    public class PrometheusEndpointMiddlewareTest : BaseTest
    {
        [Fact]
        public async void HandlePrometheusRequestAsync_ReturnsExpected()
        {
            var opts = new PrometheusEndpointOptions();
            var mopts = new ActuatorManagementOptions();
            mopts.EndpointOptions.Add(opts);
            var exporter = new PrometheusExporter();
            var processor = new SteeltoeProcessor(exporter);
            var factory = AutoCollectingMeterFactory.Create(processor);
            var meter = factory.GetMeter("Test");
            SetupTestView(meter);
            factory.CollectAllMetrics();
            processor.ExportMetrics();

            Task.Delay(1000).Wait();

            var ep = new PrometheusScraperEndpoint(opts, exporter);
            var middle = new PrometheusScraperEndpointMiddleware(null, ep, mopts);

            var context = CreateRequest("GET", "/actuator/prometheus");

            await middle.HandleMetricsRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var rdr = new StreamReader(context.Response.Body);
            var text = await rdr.ReadToEndAsync();
            Assert.Equal("# HELP test Testtest\n# TYPE test counter\ntest{a=\"v1\",b=\"v1\",c=\"v1\"} 45\n", text);
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

            /*var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;

            ITagKey aKey = TagKey.Create("a");
            ITagKey bKey = TagKey.Create("b");
            ITagKey cKey = TagKey.Create("c");

            string viewName = "test.test";
            IMeasureDouble measure = MeasureDouble.Create(Guid.NewGuid().ToString(), "test", MeasureUnit.Bytes);

            IViewName testViewName = ViewName.Create(viewName);
            IView testView = View.Create(
                                        testViewName,
                                        "test",
                                        measure,
                                        Sum.Create(),
                                        new List<ITagKey>() { aKey, bKey, cKey });

            stats.ViewManager.RegisterView(testView);

            ITagContext context1 = tagger
                .EmptyBuilder
                .Put(TagKey.Create("a"), TagValue.Create("v1"))
                .Put(TagKey.Create("b"), TagValue.Create("v1"))
                .Put(TagKey.Create("c"), TagValue.Create("v1"))
                .Build();

            for (int i = 0; i < 10; i++)
            {
                stats.StatsRecorder.NewMeasureMap().Put(measure, i).Record(context1);
            }

        */
        }
    }
}
