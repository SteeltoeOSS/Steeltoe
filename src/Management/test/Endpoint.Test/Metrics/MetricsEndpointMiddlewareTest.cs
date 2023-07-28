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

public sealed class MetricsEndpointMiddlewareTest : BaseTest
{
    private readonly MetricsExporterOptions _scraperOptions = new()
    {
        CacheDurationMilliseconds = 500
    };

    [Fact]
    public void ParseTag_ReturnsExpected()
    {
        IOptionsMonitor<MetricsEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>();

        var handler = new MetricsEndpointHandler(endpointOptionsMonitor, new SteeltoeExporter(_scraperOptions), NullLoggerFactory.Instance);

        var middleware = new MetricsEndpointMiddleware(handler, managementOptionsMonitor, NullLoggerFactory.Instance);

        Assert.Null(middleware.ParseTag("foobar"));
        Assert.Equal(new KeyValuePair<string, string>("foo", "bar"), middleware.ParseTag("foo:bar"));
        Assert.Equal(new KeyValuePair<string, string>("foo", "bar:bar"), middleware.ParseTag("foo:bar:bar"));
        Assert.Null(middleware.ParseTag("foo,bar"));
    }

    [Fact]
    public void ParseTags_ReturnsExpected()
    {
        IOptionsMonitor<MetricsEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>();

        var handler = new MetricsEndpointHandler(endpointOptionsMonitor, new SteeltoeExporter(_scraperOptions), NullLoggerFactory.Instance);

        var middleware = new MetricsEndpointMiddleware(handler, managementOptionsMonitor, NullLoggerFactory.Instance);

        HttpContext context1 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class", "?foo=key:value");
        IList<KeyValuePair<string, string>> result = middleware.ParseTags(context1.Request.Query);
        Assert.NotNull(result);
        Assert.Empty(result);

        HttpContext context2 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class", "?tag=key:value");
        result = middleware.ParseTags(context2.Request.Query);
        Assert.NotNull(result);
        Assert.Contains(new KeyValuePair<string, string>("key", "value"), result);

        HttpContext context3 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class", "?tag=key:value&foo=key:value&tag=key1:value1");
        result = middleware.ParseTags(context3.Request.Query);
        Assert.NotNull(result);
        Assert.Contains(new KeyValuePair<string, string>("key", "value"), result);
        Assert.Contains(new KeyValuePair<string, string>("key1", "value1"), result);
        Assert.Equal(2, result.Count);

        HttpContext context4 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class", "?tag=key:value&foo=key:value&tag=key:value");
        result = middleware.ParseTags(context4.Request.Query);
        Assert.NotNull(result);
        Assert.Contains(new KeyValuePair<string, string>("key", "value"), result);
        Assert.Single(result);
    }

    [Fact]
    public void GetMetricName_ReturnsExpected()
    {
        IOptionsMonitor<MetricsEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>();

        var handler = new MetricsEndpointHandler(endpointOptionsMonitor, new SteeltoeExporter(_scraperOptions), NullLoggerFactory.Instance);

        var middleware = new MetricsEndpointMiddleware(handler, managementOptionsMonitor, NullLoggerFactory.Instance);

        HttpContext context1 = CreateRequest("GET", "/cloudfoundryapplication/metrics");
        Assert.Null(middleware.GetMetricName(context1.Request));

        HttpContext context2 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class");
        Assert.Equal("Foo.Bar.Class", middleware.GetMetricName(context2.Request));

        HttpContext context3 = CreateRequest("GET", "/cloudfoundryapplication/metrics", "?tag=key:value&tag=key1:value1");
        Assert.Null(middleware.GetMetricName(context3.Request));
    }

    [Fact]
    public void GetMetricName_ReturnsExpected_When_ManagementPath_Is_Slash()
    {
        IOptionsMonitor<MetricsEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>();

        var handler = new MetricsEndpointHandler(endpointOptionsMonitor, new SteeltoeExporter(_scraperOptions), NullLoggerFactory.Instance);

        var middleware = new MetricsEndpointMiddleware(handler, managementOptionsMonitor, NullLoggerFactory.Instance);

        HttpContext context1 = CreateRequest("GET", "/actuator/metrics");
        Assert.Null(middleware.GetMetricName(context1.Request));

        HttpContext context2 = CreateRequest("GET", "/actuator/metrics/Foo.Bar.Class");
        Assert.Equal("Foo.Bar.Class", middleware.GetMetricName(context2.Request));

        HttpContext context3 = CreateRequest("GET", "/actuator/metrics", "?tag=key:value&tag=key1:value1");
        Assert.Null(middleware.GetMetricName(context3.Request));
    }

    [Fact]
    public async Task HandleMetricsRequestAsync_GetMetricsNames_ReturnsExpected()
    {
        Dictionary<string, string> appSettings = new()
        {
            ["management:endpoints:actuator:exposure:include:0"] = "*"
        };

        IOptionsMonitor<MetricsEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>(appSettings);

        SteeltoeMetrics.InstrumentationName = Guid.NewGuid().ToString();
        var exporter = new SteeltoeExporter(_scraperOptions);

        GetTestMetrics(exporter);

        var handler = new MetricsEndpointHandler(endpointOptionsMonitor, exporter, NullLoggerFactory.Instance);

        var middleware = new MetricsEndpointMiddleware(handler, managementOptions, NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("GET", "/cloudfoundryapplication/metrics");

        await middleware.InvokeAsync(context, null);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        string json = await reader.ReadToEndAsync();
        Assert.Equal("{\"names\":[]}", json);
    }

    [Fact]
    public async Task HandleMetricsRequestAsync_GetSpecificNonExistingMetric_ReturnsExpected()
    {
        IOptionsMonitor<MetricsEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>();

        var exporter = new SteeltoeExporter(_scraperOptions);

        var handler = new MetricsEndpointHandler(endpointOptionsMonitor, exporter, NullLoggerFactory.Instance);

        GetTestMetrics(exporter);
        var middleware = new MetricsEndpointMiddleware(handler, managementOptionsMonitor, NullLoggerFactory.Instance);

        HttpContext context = CreateRequest("GET", "/cloudfoundryapplication/metrics/foo.bar");

        await middleware.InvokeAsync(context, null);
        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task HandleMetricsRequestAsync_GetSpecificExistingMetric_ReturnsExpected()
    {
        IOptionsMonitor<MetricsEndpointOptions> endpointOptionsMonitor = GetOptionsMonitorFromSettings<MetricsEndpointOptions>();
        IOptionsMonitor<ManagementOptions> managementOptionsMonitor = GetOptionsMonitorFromSettings<ManagementOptions>();

        var exporter = new SteeltoeExporter(_scraperOptions);
        AggregationManager aggManager = GetTestMetrics(exporter);
        aggManager.Start();
        var handler = new MetricsEndpointHandler(endpointOptionsMonitor, exporter, NullLoggerFactory.Instance);

        var middleware = new MetricsEndpointMiddleware(handler, managementOptionsMonitor, NullLoggerFactory.Instance);

        SetupTestView();

        HttpContext context = CreateRequest("GET", "/cloudfoundryapplication/metrics/test", "?tag=a:v1");

        await middleware.InvokeAsync(context, null);
        Assert.Equal(200, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        string json = await reader.ReadToEndAsync();

        Assert.Equal(
            "{\"name\":\"test\",\"measurements\":[{\"statistic\":\"Rate\",\"value\":45}],\"availableTags\":[{\"tag\":\"a\",\"values\":[\"v1\"]},{\"tag\":\"b\",\"values\":[\"v1\"]},{\"tag\":\"c\",\"values\":[\"v1\"]}]}",
            json);
    }

    [Fact]
    public void RoutesByPathAndVerb()
    {
        var endpointOptions = GetOptionsFromSettings<MetricsEndpointOptions>();
        ManagementOptions managementOptions = GetOptionsMonitorFromSettings<ManagementOptions>().CurrentValue;

        Assert.False(endpointOptions.RequiresExactMatch());
        Assert.Equal("/actuator/metrics/{**_}", endpointOptions.GetPathMatchPattern(managementOptions, managementOptions.Path));

        Assert.Equal("/cloudfoundryapplication/metrics/{**_}",
            endpointOptions.GetPathMatchPattern(managementOptions, ConfigureManagementOptions.DefaultCloudFoundryPath));

        Assert.Contains("Get", endpointOptions.AllowedVerbs);
    }

    private HttpContext CreateRequest(string method, string path, string query = null)
    {
        HttpContext context = new DefaultHttpContext
        {
            TraceIdentifier = Guid.NewGuid().ToString()
        };

        context.Response.Body = new MemoryStream();
        context.Request.Method = method;
        context.Request.Path = path;
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

        for (int index = 0; index < 10; index++)
        {
            counter.Add(index, new ReadOnlySpan<KeyValuePair<string, object>>(labels.ToArray()));
        }
    }
}
