using OpenTelemetry.Metrics;
using System.Collections.Generic;

namespace Steeltoe.Management.OpenTelemetry.Metrics
{
    public interface IViewRegistry
    {
        List<KeyValuePair<string, MetricStreamConfiguration>> Views { get; }

        void AddView(string instrumentName, MetricStreamConfiguration viewConfig);
    }
}