// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenCensus.Stats;
using OpenCensus.Tags;
using Steeltoe.Management.Census.Stats;
using System.Collections.Generic;

namespace Steeltoe.Management.Exporter.Metrics.CloudFoundryForwarder
{
    public abstract class CloudFoundryMetricWriter : ICloudFoundryMetricWriter
    {
        protected readonly CloudFoundryForwarderOptions options;
        protected readonly IStats stats;
        protected readonly ILogger logger;

        public CloudFoundryMetricWriter(CloudFoundryForwarderOptions options, IStats stats, ILogger logger = null)
        {
            this.options = options;
            this.stats = stats;
            this.logger = logger;
        }

        public abstract IList<Metric> CreateMetrics(IViewData viewData, IAggregationData aggregation, TagValues tagValues, long timeStamp);

        protected internal IDictionary<string, string> GetTagKeysAndValues(IList<ITagKey> keys, IList<ITagValue> values)
        {
            IDictionary<string, string> result = new SortedDictionary<string, string>();

            if (keys.Count != values.Count)
            {
                logger?.LogWarning("TagKeys and TagValues don't have same size., ignoring tags");
                return result;
            }

            for (int i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var val = values[i];
                if (val != null)
                {
                    result.Add(key.Name, val.AsString);
                }
            }

            return result;
        }
    }
}
