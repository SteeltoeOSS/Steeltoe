// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using System;

namespace Steeltoe.Management.OpenTelemetry.Exporters.Wavefront;

public class WavefrontApplicationOptions
{
    internal const string WAVEFRONT_PREFIX = "wavefront:application";

    public WavefrontApplicationOptions(IConfiguration config)
    {
        var section = config?.GetSection(WAVEFRONT_PREFIX) ?? throw new ArgumentNullException(nameof(config));
        section.Bind(this);
    }

    public string Source { get; set; }

    public string Name { get; set; }

    public string Service { get; set; }

    public string Cluster { get; set; }
}
