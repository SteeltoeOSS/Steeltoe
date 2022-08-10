// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using OpenTelemetry.Metrics;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.OpenTelemetry.Exporters;
using Steeltoe.Management.OpenTelemetry.Metrics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Test;

[Obsolete("To be removed in the next major version.")]
public class PrometheusEndpointMiddlewareTest : BaseTest
{
    [Fact]
    public async Task HandlePrometheusRequestAsync_ReturnsExpected()
    {
        var opts = new PrometheusEndpointOptions();
        var managementOptions = new ActuatorManagementOptions();
        managementOptions.EndpointOptions.Add(opts);
        var exporter = new SteeltoePrometheusExporter();
        var viewRegistry = new ViewRegistry();
        using MeterProvider metrics = GetTestMetrics(viewRegistry, null, exporter, "test1", "1.0");

        var ep = new PrometheusScraperEndpoint(opts, new List<MetricsExporter>
        {
            exporter
        });

        var middle = new PrometheusScraperEndpointMiddleware(null, ep, managementOptions);
        var meter = new Meter("test1", "1.0");
        Counter<double> measure = meter.CreateCounter<double>("test");

        var labels = new Dictionary<string, object>
        {
            { "a", "v1" },
            { "b", "v1" },
            { "c", "v1" }
        };

        for (int i = 0; i < 10; i++)
        {
            measure.Add(i, new ReadOnlySpan<KeyValuePair<string, object>>(labels.ToArray()));
        }

        Task.Delay(1000).Wait();
        HttpContext context = CreateRequest("GET", "/actuator/prometheus");

        await middle.HandleMetricsRequestAsync(context);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var rdr = new StreamReader(context.Response.Body);
        string text = await rdr.ReadToEndAsync();
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
}
