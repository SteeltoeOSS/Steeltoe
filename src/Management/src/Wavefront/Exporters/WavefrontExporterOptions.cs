// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Wavefront.Exporters;

public sealed class WavefrontExporterOptions
{
    public string? Uri { get; set; }
    public string? ApiToken { get; set; }
    public int Step { get; set; } = 30_000; // milliseconds
    public int BatchSize { get; set; } = 10_000;
    public int MaxQueueSize { get; set; } = 500_000;
    public string? Source { get; set; }
    public string? Name { get; set; }
    public string? Service { get; set; }
}
