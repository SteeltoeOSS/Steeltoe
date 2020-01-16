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
    public class PrometheusEndpointMiddlewareTest : BaseTest
    {
        [Fact]
        public async void HandlePrometheusRequestAsync_ReturnsExpected()
        {
            var opts = new PrometheusEndpointOptions();
            var mopts = TestHelper.GetManagementOptions(opts);
            var stats = new OpenCensusStats();
            SetupTestView(stats);
            var ep = new PrometheusScraperEndpoint(opts, stats);
            var middle = new PrometheusScraperEndpointMiddleware(null, ep, mopts);

            var context = CreateRequest("GET", "/actuator/prometheus");

            await middle.HandleMetricsRequestAsync(context);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var rdr = new StreamReader(context.Response.Body);
            var text = await rdr.ReadToEndAsync();
            Assert.Equal("# HELP test_test test\n# TYPE test_test gauge\ntest_test{a=\"v1\",b=\"v1\",c=\"v1\"} 45\n", text);
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
