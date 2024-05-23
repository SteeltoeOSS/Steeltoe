// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Wavefront.Exporters;

public sealed class WavefrontApplicationOptions
{
    public string? Source { get; set; }
    public string? Name { get; set; }
    public string? Service { get; set; }
}
