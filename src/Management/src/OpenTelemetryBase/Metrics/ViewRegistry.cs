// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Metrics;
using System.Collections.Generic;

namespace Steeltoe.Management.OpenTelemetry.Metrics
{
    public class ViewRegistry : IViewRegistry
    {
        public ViewRegistry()
        {
            Views = new List<KeyValuePair<string, MetricStreamConfiguration>>();
        }

        public List<KeyValuePair<string, MetricStreamConfiguration>> Views { get; }

        public void AddView(string instrumentName, MetricStreamConfiguration viewConfig)
        {
            Views.Add(new KeyValuePair<string, MetricStreamConfiguration>(instrumentName, viewConfig));
        }
    }
}
