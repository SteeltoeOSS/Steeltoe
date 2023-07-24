// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Metrics;
using System.Reflection;

namespace Steeltoe.Management.MetricCollectors.Metrics;

internal static class SteeltoeMetrics
{
    private static readonly AssemblyName AssemblyName = typeof(SteeltoeMetrics).Assembly.GetName();
    private static readonly string? InstrumentationVersion = AssemblyName.Version?.ToString();

    public static Meter Meter => new(InstrumentationName, InstrumentationVersion);
    public static string InstrumentationName { get; set; } = AssemblyName.Name ?? throw new InvalidOperationException(nameof(InstrumentationName));
}
