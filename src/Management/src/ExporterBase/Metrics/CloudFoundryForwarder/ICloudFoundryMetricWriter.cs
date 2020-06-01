// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenCensus.Stats;
using OpenCensus.Tags;
using System.Collections.Generic;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder
{
    public interface ICloudFoundryMetricWriter
    {
        IList<Metric> CreateMetrics(IViewData viewData, IAggregationData aggregation, TagValues tagValues, long timeStamp);
    }
}
