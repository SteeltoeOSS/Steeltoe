// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace Steeltoe.Management.OpenTelemetry.Exporters;

public abstract class MetricsExporter : BaseExporter<Metric>, IPullMetricExporter
{
    internal abstract int ScrapeResponseCacheDurationMilliseconds { get; }

    internal abstract Func<Batch<Metric>, ExportResult> OnExport { get; set; }

    public abstract Func<int, bool> Collect { get; set; }

    internal abstract ICollectionResponse GetCollectionResponse(Batch<Metric> metrics = default);

    internal abstract ICollectionResponse GetCollectionResponse(ICollectionResponse collectionResponse, DateTime updatedTime);
}
