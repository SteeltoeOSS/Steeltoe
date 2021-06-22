// <copyright file="OpenTelemetrySdkEventSource.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Threading;
//using OpenTelemetry.Trace.Export;

namespace OpenTelemetry.Internal
{
    /// <summary>
    /// EventSource implementation for OpenTelemetry SDK implementation.
    /// </summary>
    [EventSource(Name = "OpenTelemetry-Sdk")]
    internal class OpenTelemetrySdkEventSource : EventSource
    {
        public static OpenTelemetrySdkEventSource Log = new OpenTelemetrySdkEventSource();

        [NonEvent]
        public void MetricObserverCallbackException(string metricName, Exception ex)
        {
            if (this.IsEnabled(EventLevel.Warning, (EventKeywords)(-1)))
            {
                this.MetricObserverCallbackError(metricName, ToInvariantString(ex));
            }
        }

        [NonEvent]
        public void MetricControllerException(Exception ex)
        {
            if (this.IsEnabled(EventLevel.Warning, (EventKeywords)(-1)))
            {
                this.MetricControllerException(ToInvariantString(ex));
            }
        }

        [Event(3, Message = "Exporter returned error '{0}'.", Level = EventLevel.Warning)]
        public void ExporterErrorResult(ExportResult exportResult)
        {
            this.WriteEvent(3, exportResult.ToString());
        }

        [Event(16, Message = "Exception occurring while invoking Metric Observer callback. '{0}' Exception: '{1}'", Level = EventLevel.Warning)]
        public void MetricObserverCallbackError(string metricName, string exception)
        {
            this.WriteEvent(16, metricName, exception);
        }

        [Event(19, Message = "Exception occurred in Metric Controller while processing metrics from one Collect cycle. This does not shutdown controller and subsequent collections will be done. Exception: '{0}'", Level = EventLevel.Warning)]
        public void MetricControllerException(string exception)
        {
            this.WriteEvent(19, exception);
        }

        [Event(20, Message = "Meter Collect Invoked for Meter: '{0}'", Level = EventLevel.Verbose)]
        public void MeterCollectInvoked(string meterName)
        {
            this.WriteEvent(20, meterName);
        }

        [Event(21, Message = "Metric Export failed with error '{0}'.", Level = EventLevel.Warning)]
        public void MetricExporterErrorResult(int exportResult)
        {
            this.WriteEvent(21, exportResult);
        }

        /// <summary>
        /// Returns a culture-independent string representation of the given <paramref name="exception"/> object,
        /// appropriate for diagnostics tracing.
        /// </summary>
        private static string ToInvariantString(Exception exception)
        {
            var originalUICulture = Thread.CurrentThread.CurrentUICulture;

            try
            {
                Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
                return exception.ToString();
            }
            finally
            {
                Thread.CurrentThread.CurrentUICulture = originalUICulture;
            }
        }
    }
}
