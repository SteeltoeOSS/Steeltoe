// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using OpenCensus.Stats;
using OpenCensus.Stats.Aggregations;
using OpenCensus.Stats.Measures;
using OpenCensus.Tags;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Endpoint.Test;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Test
{
    public class MetricsEndpointMiddlewareTest : BaseTest
    {
        [Fact]
        public void ParseTag_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
            var stats = new OpenCensusStats();

            var ep = new MetricsEndpoint(opts, stats);
            var middle = new MetricsEndpointMiddleware(null, ep, mopts);

            Assert.Null(middle.ParseTag("foobar"));
            Assert.Equal(new KeyValuePair<string, string>("foo", "bar"), middle.ParseTag("foo:bar"));
            Assert.Equal(new KeyValuePair<string, string>("foo", "bar:bar"), middle.ParseTag("foo:bar:bar"));
            Assert.Null(middle.ParseTag("foo,bar"));
        }

        [Fact]
        public void ParseTags_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
            var stats = new OpenCensusStats();

            var ep = new MetricsEndpoint(opts, stats);
            var middle = new MetricsEndpointMiddleware(null, ep, mopts);

            var context1 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class", "?foo=key:value");
            var result = middle.ParseTags(context1.Request.Query);
            Assert.NotNull(result);
            Assert.Empty(result);

            var context2 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class", "?tag=key:value");
            result = middle.ParseTags(context2.Request.Query);
            Assert.NotNull(result);
            Assert.Contains(new KeyValuePair<string, string>("key", "value"), result);

            var context3 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class", "?tag=key:value&foo=key:value&tag=key1:value1");
            result = middle.ParseTags(context3.Request.Query);
            Assert.NotNull(result);
            Assert.Contains(new KeyValuePair<string, string>("key", "value"), result);
            Assert.Contains(new KeyValuePair<string, string>("key1", "value1"), result);
            Assert.Equal(2, result.Count);

            var context4 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class", "?tag=key:value&foo=key:value&tag=key:value");
            result = middle.ParseTags(context4.Request.Query);
            Assert.NotNull(result);
            Assert.Contains(new KeyValuePair<string, string>("key", "value"), result);
            Assert.Single(result);
        }

        [Fact]
        public void GetMetricName_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
            var stats = new OpenCensusStats();

            var ep = new MetricsEndpoint(opts, stats);
            var middle = new MetricsEndpointMiddleware(null, ep, mopts);

            var context1 = CreateRequest("GET", "/cloudfoundryapplication/metrics");
            Assert.Null(middle.GetMetricName(context1.Request));

            var context2 = CreateRequest("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class");
            Assert.Equal("Foo.Bar.Class", middle.GetMetricName(context2.Request));

            var context3 = CreateRequest("GET", "/cloudfoundryapplication/metrics", "?tag=key:value&tag=key1:value1");
            Assert.Null(middle.GetMetricName(context3.Request));
        }

        [Fact]
        public async void HandleMetricsRequestAsync_GetMetricsNames_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
            var stats = new OpenCensusStats();

            var ep = new MetricsEndpoint(opts, stats);
            var middle = new MetricsEndpointMiddleware(null, ep, mopts);

            var context = CreateRequest("GET", "/cloudfoundryapplication/metrics");

            await middle.HandleMetricsRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            StreamReader rdr = new StreamReader(context.Response.Body);
            string json = await rdr.ReadToEndAsync();
            Assert.Equal("{\"names\":[]}", json);
        }

        [Fact]
        public async void HandleMetricsRequestAsync_GetSpecificNonExistingMetric_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
            var stats = new OpenCensusStats();

            var ep = new MetricsEndpoint(opts, stats);
            var middle = new MetricsEndpointMiddleware(null, ep, mopts);

            var context = CreateRequest("GET", "/cloudfoundryapplication/metrics/foo.bar");

            await middle.HandleMetricsRequestAsync(context);
            Assert.Equal(404, context.Response.StatusCode);
        }

        [Fact]
        public async void HandleMetricsRequestAsync_GetSpecificExistingMetric_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
            var stats = new OpenCensusStats();
            var tagsComponent = new TagsComponent();
            var tagger = tagsComponent.Tagger;
            var ep = new MetricsEndpoint(opts, stats);

            SetupTestView(stats);

            var middle = new MetricsEndpointMiddleware(null, ep, mopts);

            var context = CreateRequest("GET", "/cloudfoundryapplication/metrics/test.test", "?tag=a:v1");

            await middle.HandleMetricsRequestAsync(context);
            Assert.Equal(200, context.Response.StatusCode);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            StreamReader rdr = new StreamReader(context.Response.Body);
            string json = await rdr.ReadToEndAsync();
            Assert.Equal("{\"name\":\"test.test\",\"measurements\":[{\"statistic\":\"TOTAL\",\"value\":45.0}],\"availableTags\":[{\"tag\":\"a\",\"values\":[\"v1\"]},{\"tag\":\"b\",\"values\":[\"v1\"]},{\"tag\":\"c\",\"values\":[\"v1\"]}]}", json);
        }

        [Fact]
        public void MetricsEndpointMiddleware_PathAndVerbMatching_ReturnsExpected()
        {
            var opts = new MetricsEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
            var stats = new OpenCensusStats();
            var ep = new MetricsEndpoint(opts, stats);
            var middle = new MetricsEndpointMiddleware(null, ep, mopts);

            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/metrics"));
            Assert.False(middle.RequestVerbAndPathMatch("PUT", "/cloudfoundryapplication/metrics"));
            Assert.False(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/badpath"));
            Assert.False(middle.RequestVerbAndPathMatch("POST", "/cloudfoundryapplication/metrics"));
            Assert.False(middle.RequestVerbAndPathMatch("DELETE", "/cloudfoundryapplication/metrics"));
            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class"));
            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/metrics/Foo.Bar.Class?tag=key:value&tag=key1:value1"));
            Assert.True(middle.RequestVerbAndPathMatch("GET", "/cloudfoundryapplication/metrics?tag=key:value&tag=key1:value1"));
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

        private void SetupTestView(OpenCensusStats stats)
        {
            var tagsComponent = new TagsComponent();
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
        }
    }
}
