// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;
using OpenCensus.Stats;
using OpenCensus.Stats.Measures;
using OpenCensus.Tags;
using Steeltoe.Management.Census.Stats;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder.Test
{
    public class BaseTest
    {
        public string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(
                value,
                new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }

        public void SetupTestView(OpenCensusStats stats, IAggregation agg, IMeasure measure = null, string viewName = "test.test")
        {
            var aKey = TagKey.Create("a");
            var bKey = TagKey.Create("b");
            var cKey = TagKey.Create("c");

            if (measure == null)
            {
                measure = MeasureDouble.Create(Guid.NewGuid().ToString(), "test", MeasureUnit.Bytes);
            }

            var testViewName = ViewName.Create(viewName);
            var testView = View.Create(
                                        testViewName,
                                        "test",
                                        measure,
                                        agg,
                                        new List<ITagKey>() { aKey, bKey, cKey });

            stats.ViewManager.RegisterView(testView);
        }
    }
}
