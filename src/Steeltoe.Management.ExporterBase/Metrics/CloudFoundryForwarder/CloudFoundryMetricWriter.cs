// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Census.Stats;
using Steeltoe.Management.Census.Tags;
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
