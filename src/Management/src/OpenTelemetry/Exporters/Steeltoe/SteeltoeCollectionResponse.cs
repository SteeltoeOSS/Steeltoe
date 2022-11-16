// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.OpenTelemetry.Metrics;

namespace Steeltoe.Management.OpenTelemetry.Exporters.Steeltoe;

public record struct SteeltoeCollectionResponse(MetricsCollection<List<MetricSample>> MetricSamples, MetricsCollection<List<MetricTag>> AvailableTags,
    DateTime GeneratedAtUtc) : ICollectionResponse;
