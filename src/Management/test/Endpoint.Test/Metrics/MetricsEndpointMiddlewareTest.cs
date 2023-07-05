// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Metrics;
using Steeltoe.Management.Endpoint.Options;
using Steeltoe.Management.MetricCollectors.Aggregations;
using Steeltoe.Management.MetricCollectors.Exporters;
using Steeltoe.Management.MetricCollectors.Exporters.Steeltoe;
using Steeltoe.Management.MetricCollectors.Metrics;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.Metrics;

public class MetricsEndpointMiddlewareTest : BaseTest
{
    private readonly MetricsExporterOptions _scraperOptions = new()
    {
        MetricsCacheDurationMilliseconds = 500
    };

    [Fact]
    public void ParseTag_ReturnsExpected()
    {
        IOptionsMonitor<MetricsEndpointOptions> opts = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();

        var ep = new MetricsEndpointHandler(opts, new SteeltoeExporter(_scraperOptions), NullLoggerFactory.Instance);

        var middle = new MetricsEndpointMiddleware(ep, managementOptions, NullLoggerFactory.Instance);

        Assert.Null(middle.ParseTag("foobar"));
        Assert.Equal(new KeyValuePair<string, string>("foo", "bar"), middle.ParseTag("foo:bar"));
        Assert.Equal(new KeyValuePair<string, string>("foo", "bar:bar"), middle.ParseTag("foo:bar:bar"));
        Assert.Null(middle.ParseTag("foo,bar"));
    }

    [Fact]
    public void ParseTags_ReturnsExpected()
    {
        IOptionsMonitor<MetricsEndpointOptions> opts = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();

        var ep = new MetricsEndpointHandler(opts, new SteeltoeExporter(_scraperOptions), NullLoggerFactory.Instance);

        var middle = new MetricsEndpointMiddleware(ep, managementOptions, NullLoggerFactory.Instance);

        HttpContext context1 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class", "?foo=key:value");
        IList<KeyValuePair<string, string>> result = middle.ParseTags(context1.Request.Query);
        Assert.NotNull(result);
        Assert.Empty(result);

        HttpContext context2 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class", "?tag=key:value");
        result = middle.ParseTags(context2.Request.Query);
        Assert.NotNull(result);
        Assert.Contains(new KeyValuePair<string, string>("key", "value"), result);

        HttpContext context3 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class", "?tag=key:value&foo=key:value&tag=key1:value1");
        result = middle.ParseTags(context3.Request.Query);
        Assert.NotNull(result);
        Assert.Contains(new KeyValuePair<string, string>("key", "value"), result);
        Assert.Contains(new KeyValuePair<string, string>("key1", "value1"), result);
        Assert.Equal(2, result.Count);

        HttpContext context4 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class", "?tag=key:value&foo=key:value&tag=key:value");
        result = middle.ParseTags(context4.Request.Query);
        Assert.NotNull(result);
        Assert.Contains(new KeyValuePair<string, string>("key", "value"), result);
        Assert.Single(result);
    }

    [Fact]
    public void GetMetricName_ReturnsExpected()
    {
        IOptionsMonitor<MetricsEndpointOptions> opts = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();

        IOptionsMonitor<ManagementEndpointOptions> managementOptions =
            GetOptionsMonitorFromSettings<ManagementEndpointOptions, ConfigureTestManagementOptions>();

        var ep = new MetricsEndpointHandler(opts, new SteeltoeExporter(_scraperOptions), NullLoggerFactory.Instance);

        var middle = new MetricsEndpointMiddleware(ep, managementOptions, NullLoggerFactory.Instance);

        HttpContext context1 = CreateRequest("GET", "/cloudfoundryapplication/metrics");
        Assert.Null(middle.GetMetricName(context1.Request));

        HttpContext context2 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class");
        Assert.Equal("Foo.Bar.Class", middle.GetMetricName(context2.Request));

        HttpContext context3 = CreateRequest("GET", "/cloudfoundryapplication/metrics", "?tag=key:value&tag=key1:value1");
        Assert.Null(middle.GetMetricName(context3.Request));
    }

    [Fact]
    public void GetMetricName_ReturnsExpected_When_ManagementPath_Is_Slash()
    {
        IOptionsMonitor<MetricsEndpointOptions> opts = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();

        var ep = new MetricsEndpointHandler(opts, new SteeltoeExporter(_scraperOptions), NullLoggerFactory.Instance);

        var middle = new MetricsEndpointMiddleware(ep, managementOptions, NullLoggerFactory.Instance);

        HttpContext context1 = CreateRequest("GET", "/actuator/metrics");
        Assert.Null(middle.GetMetricName(context1.Request));

        HttpContext context2 = CreateRequest("GET", "/actuator/metrics/Foo.Bar.Class");
        Assert.Equal("Foo.Bar.Class", middle.GetMetricName(context2.Request));

        HttpContext context3 = CreateRequest("GET", "/actuator/metrics", "?tag=key:value&tag=key1:value1");
        Assert.Null(middle.GetMetricName(context3.Request));
    }

    [Fact]
    public async Task HandleMetricsRequestAsync_GetMetricsNames_ReturnsExpected()
    {
        Dictionary<string, string> appSettings = new()
        {
            ["management:endpoints:actuator:exposure:include:0"] = "*"
        };

        IOptionsMonitor<MetricsEndpointOptions> opts = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>(appSettings);

        SteeltoeMetrics.InstrumentationName = Guid.NewGuid().ToString();
        var exporter = new SteeltoeExporter(_scraperOptions);

        GetTestMetrics(exporter);

        var ep = new MetricsEndpointHandler(opts, exporter, NullLoggerFactory.Instance);

        var middle = new MetricsEndpointMiddleware(ep, managementOptions, NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("GET", "/cloudfoundryapplication/metrics");

        await middle.InvokeAsync(context, null);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var rdr = new StreamReader(context.Response.Body);
        string json = await rdr.ReadToEndAsync();
        Assert.Equal("{\"names\":[]}", json);
    }

    [Fact]
    public async Task HandleMetricsRequestAsync_GetSpecificNonExistingMetric_ReturnsExpected()
    {
        IOptionsMonitor<MetricsEndpointOptions> opts = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();

        IOptionsMonitor<ManagementEndpointOptions> managementOptions =
            GetOptionsMonitorFromSettings<ManagementEndpointOptions, ConfigureTestManagementOptions>();

        var exporter = new SteeltoeExporter(_scraperOptions);

        var ep = new MetricsEndpointHandler(opts, exporter, NullLoggerFactory.Instance);

        GetTestMetrics(exporter);
        var middle = new MetricsEndpointMiddleware(ep, managementOptions, NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("GET", "/cloudfoundryapplication/metrics/foo.bar");

        await middle.InvokeAsync(context, null);
        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleMetricsRequestAsync_GetSpecificExistingMetric_ReturnsExpected()
    {
        IOptionsMonitor<MetricsEndpointOptions> opts = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();

        IOptionsMonitor<ManagementEndpointOptions> managementOptions =
            GetOptionsMonitorFromSettings<ManagementEndpointOptions, ConfigureTestManagementOptions>();

        var exporter = new SteeltoeExporter(_scraperOptions);
        AggregationManager aggManager = GetTestMetrics(exporter);
        aggManager.Start();
        var ep = new MetricsEndpointHandler(opts, exporter, NullLoggerFactory.Instance);

        var middle = new MetricsEndpointMiddleware(ep, managementOptions, NullLoggerFactory.Instance);

        SetupTestView();

        HttpContext context = CreateRequest("GET", "/cloudfoundryapplication/metrics/test", "?tag=a:v1");

        await middle.InvokeAsync(context, null);
        Assert.Equal(200, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var rdr = new StreamReader(context.Response.Body);
        string json = await rdr.ReadToEndAsync();

        Assert.Equal(
            "{\"name\":\"test\",\"measurements\":[{\"statistic\":\"Rate\",\"value\":45}],\"availableTags\":[{\"tag\":\"a\",\"values\":[\"v1\"]},{\"tag\":\"b\",\"values\":[\"v1\"]},{\"tag\":\"c\",\"values\":[\"v1\"]}]}",
            json);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var options = GetOptionsFromSettings<MetricsEndpointOptions>();
        IOptionsMonitor<ManagementEndpointOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementEndpointOptions>();
        Assert.False(options.ExactMatch);
        Assert.Equal("/actuator/metrics/{**_}", options.GetContextPath(managementOptions.Get(EndpointContexts.Actuator)));
        Assert.Equal("/cloudfoundryapplication/metrics/{**_}", options.GetContextPath(managementOptions.Get(EndpointContexts.CloudFoundry)));
        Assert.Contains("Get", options.AllowedVerbs);
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

    private void SetupTestView()
    {
        Counter<double> counter = SteeltoeMetrics.Meter.CreateCounter<double>("test");

        var labels = new Dictionary<string, object>
        {
            { "a", "v1" },
            { "b", "v1" },
            { "c", "v1" }
        };

        for (int i = 0; i < 10; i++)
        {
            counter.Add(i, new ReadOnlySpan<KeyValuePair<string, object>>(labels.ToArray()));
        }
    }
}
