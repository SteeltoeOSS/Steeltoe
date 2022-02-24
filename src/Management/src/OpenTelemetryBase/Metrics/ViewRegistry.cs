using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;

namespace Steeltoe.Management.OpenTelemetry.Metrics
{
    public class ViewRegistry : IViewRegistry
    {
        private List<KeyValuePair<string, MetricStreamConfiguration>> _views;

        public ViewRegistry()
        {
            _views = new List<KeyValuePair<string, MetricStreamConfiguration>>();
        }

        public List<KeyValuePair<string, MetricStreamConfiguration>> Views { get => _views; }

        public void AddView(string instrumentName, MetricStreamConfiguration viewConfig)
        {
            _views.Add(new KeyValuePair<string, MetricStreamConfiguration>(instrumentName, viewConfig));
        }
    }
}
