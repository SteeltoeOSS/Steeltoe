// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Wavefront.Exporters;

public interface IWavefrontExporterOptions
{
    string ApiToken { get; set; }
    WavefrontApplicationOptions ApplicationOptions { get; }
    int BatchSize { get; set; }
    string Cluster { get; set; }
    int MaxQueueSize { get; set; }
    string Name { get; }
    string Service { get; }
    string Source { get; }
    int Step { get; set; }
    string Uri { get; set; }
}
