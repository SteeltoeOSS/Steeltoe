﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class MetricsRequest
    {
        public string MetricName { get; }

        public List<KeyValuePair<string, string>> Tags { get; }

        public MetricsRequest(string metricName, List<KeyValuePair<string, string>> tags)
        {
            MetricName = metricName;
            Tags = tags;
        }
    }
}
