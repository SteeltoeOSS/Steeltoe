// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Test
{
    [Obsolete]
    public class PrometheusEndpointMiddlewareTest : BaseTest
    {
        [Fact]
        public async Task HandlePrometheusRequestAsync_ReturnsExpected()
        {
            var opts = new PrometheusEndpointOptions();
            var mopts = new ActuatorManagementOptions();
            mopts.EndpointOptions.Add(opts);
            var exporter = new SteeltoePrometheusExporter();
            var viewRegistry = new ViewRegistry();
            using var otel = GetTestMetrics(viewRegistry, null, exporter, "test1", "1.0");

            var ep = new PrometheusScraperEndpoint(opts, new List<IMetricsExporter> { exporter });
            var middle = new PrometheusScraperEndpointMiddleware(null, ep, mopts);
            var meter = new Meter("test1", "1.0");
            var measure = meter.CreateCounter<double>("test");
            var labels = new Dictionary<string, object>
            {
                { "a", "v1" },
                { "b", "v1" },
                { "c", "v1" }
            };

            for (var i = 0; i < 10; i++)
            {
                measure.Add(i, new ReadOnlySpan<KeyValuePair<string, object>>(labels.ToArray()));
            }

            Task.Delay(1000).Wait();
            var context = CreateRequest("GET", "/actuator/prometheus");

            await middle.HandleMetricsRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var rdr = new StreamReader(context.Response.Body);
            var text = await rdr.ReadToEndAsync();
            Assert.Contains("# TYPE test counter\ntest{a=\"v1\",b=\"v1\",c=\"v1\"} 45", text);
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

        private void SetupTestView(SteeltoePrometheusExporter prometheusExporter)
        {
            /*var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;

            var aKey = TagKey.Create("a");
            var bKey = TagKey.Create("b");
            var cKey = TagKey.Create("c");

            var viewName = "test.test";
            var measure = MeasureDouble.Create(Guid.NewGuid().ToString(), "test", MeasureUnit.Bytes);

            var testViewName = ViewName.Create(viewName);
            var testView = View.Create(
                                        testViewName,
                                        "test",
                                        measure,
                                        Sum.Create(),
                                        new List<ITagKey>() { aKey, bKey, cKey });

            stats.ViewManager.RegisterView(testView);

            var context1 = tagger
                .EmptyBuilder
                .Put(TagKey.Create("a"), TagValue.Create("v1"))
                .Put(TagKey.Create("b"), TagValue.Create("v1"))
                .Put(TagKey.Create("c"), TagValue.Create("v1"))
                .Build();

            for (var i = 0; i < 10; i++)
            {
                stats.StatsRecorder.NewMeasureMap().Put(measure, i).Record(context1);
            }

        */
        }
    }
}
