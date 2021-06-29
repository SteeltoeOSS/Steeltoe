﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Test;
using Steeltoe.Management.EndpointBase.Test.Metrics;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Management.Endpoint.Metrics.Observer.Test
{
    [Obsolete]
    public class CLRRuntimeObserverTest : BaseTest
    {
        // TODO: Bring back views when available
        /*
        [Fact]
        public void Constructor_RegistersExpectedViews()
        {
            var options = new MetricsEndpointOptions();
            var meter = new TestMeter();
            var observer = new CLRRuntimeObserver(options, meter, null);

            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("clr.memory.used")));
            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("clr.gc.collections")));
            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("clr.threadpool.active")));
            Assert.NotNull(stats.ViewManager.GetView(ViewName.Create("clr.threadpool.avail")));
        }
        */

        [Fact]
        public void ProcessEvent_IgnoresNulls()
        {
            var options = new MetricsObserverOptions();
            var stats = new TestOpenTelemetryMetrics();
            var observer = new CLRRuntimeObserver(options, stats, null);

            observer.ProcessEvent("foobar", null);
            observer.ProcessEvent(CLRRuntimeObserver.HEAP_EVENT, null);
            observer.ProcessEvent(CLRRuntimeObserver.THREADS_EVENT, null);
        }

        [Fact]
        public void HandleHeapEvent_RecordsValues()
        {
            var options = new MetricsObserverOptions();
            var stats = new TestOpenTelemetryMetrics();
            var observer = new CLRRuntimeObserver(options, stats, null);
            var factory = stats.Factory;

            var metrics = new CLRRuntimeSource.HeapMetrics(1000, new List<long>() { 10, 20, 30 });
            observer.HandleHeapEvent(metrics);
            factory.CollectAllMetrics();

            var processor = stats.Processor;

            var metricName = "clr.memory.used";
            var summary = processor.GetMetricByName<long>(metricName);
            Assert.NotNull(summary);
            var mean = summary.Sum / summary.Count;
            Assert.Equal(1000, mean);
            Assert.Equal(1000, summary.Min);
            Assert.Equal(1000, summary.Max);

            metricName = "clr.gc.collections";
            var gen0Label = new Dictionary<string, string>() { { "generation", "gen0" } }.ToList();
            summary = processor.GetMetricByName<long>(metricName, gen0Label);
            Assert.NotNull(summary);
            Assert.Equal(10, summary.Sum);

            var gen1Label = new Dictionary<string, string>() { { "generation", "gen1" } }.ToList();
            summary = processor.GetMetricByName<long>(metricName, gen1Label);
            Assert.NotNull(summary);
            Assert.Equal(20, summary.Sum);

            var gen2Label = new Dictionary<string, string>() { { "generation", "gen2" } }.ToList();
            summary = processor.GetMetricByName<long>(metricName, gen2Label);
            Assert.NotNull(summary);
            Assert.Equal(30, summary.Sum);

            processor.Clear();
            observer = new CLRRuntimeObserver(options, stats, null);

            metrics = new CLRRuntimeSource.HeapMetrics(1000, new List<long>() { 10, 20, 30 });
            observer.HandleHeapEvent(metrics);
            metrics = new CLRRuntimeSource.HeapMetrics(5000, new List<long>() { 15, 25, 30 });
            observer.HandleHeapEvent(metrics);
            factory.CollectAllMetrics();

            metricName = "clr.memory.used";
            summary = processor.GetMetricByName<long>(metricName);
            Assert.Equal(5000 + 1000, summary.Sum);
            Assert.Equal(5000, summary.Max);
            Assert.Equal(1000, summary.Min);

            metricName = "clr.gc.collections";
            summary = processor.GetMetricByName<long>(metricName, gen0Label);
            Assert.Equal(15, summary.Sum);

            summary = processor.GetMetricByName<long>(metricName, gen1Label);
            Assert.NotNull(summary);
            Assert.Equal(25, summary.Sum);

            summary = processor.GetMetricByName<long>(metricName, gen2Label);
            Assert.NotNull(summary);
            Assert.Equal(30, summary.Sum);
        }

        [Fact]
        public void HandleThreadsEvent_RecordsValues()
        {
            var options = new MetricsObserverOptions();
            var stats = new TestOpenTelemetryMetrics();
            var factory = stats.Factory;
            var processor = stats.Processor;
            var observer = new CLRRuntimeObserver(options, stats, null);

            var metrics = new CLRRuntimeSource.ThreadMetrics(100, 100, 200, 200);
            observer.HandleThreadsEvent(metrics);

            factory.CollectAllMetrics();

            var metricName = "clr.threadpool.active";
            var summary = processor.GetMetricByName<long>(metricName);
            Assert.NotNull(summary);
            var mean = summary.Sum / summary.Count;
            Assert.Equal(100, mean);
            Assert.Equal(100, summary.Min);
            Assert.Equal(100, summary.Max);

            var workerLabel = new Dictionary<string, string>() { { "kind", "worker" } }.ToList();
            summary = processor.GetMetricByName<long>(metricName, workerLabel);
            Assert.NotNull(summary);
            mean = summary.Sum / summary.Count;
            Assert.Equal(100, mean);
            Assert.Equal(100, summary.Min);
            Assert.Equal(100, summary.Max);

            var comportLabel = new Dictionary<string, string>() { { "kind", "completionPort" } }.ToList();
            summary = processor.GetMetricByName<long>(metricName, comportLabel);
            Assert.NotNull(summary);
            mean = summary.Sum / summary.Count;
            Assert.Equal(100, mean);
            Assert.Equal(100, summary.Min);
            Assert.Equal(100, summary.Max);

            metricName = "clr.threadpool.avail";
            summary = processor.GetMetricByName<long>(metricName);
            Assert.NotNull(summary);
            mean = summary.Sum / summary.Count;
            Assert.Equal(100, mean);
            Assert.Equal(100, summary.Min);
            Assert.Equal(100, summary.Max);

            summary = processor.GetMetricByName<long>(metricName, workerLabel);
            Assert.NotNull(summary);
            mean = summary.Sum / summary.Count;
            Assert.Equal(100, mean);
            Assert.Equal(100, summary.Min);
            Assert.Equal(100, summary.Max);

            summary = processor.GetMetricByName<long>(metricName, comportLabel);
            Assert.NotNull(summary);
            mean = summary.Sum / summary.Count;
            Assert.Equal(100, mean);
            Assert.Equal(100, summary.Min);
            Assert.Equal(100, summary.Max);
        }
    }
}
