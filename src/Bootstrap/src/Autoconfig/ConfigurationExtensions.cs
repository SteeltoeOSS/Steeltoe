// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry.Exporters.Wavefront;

namespace Steeltoe.Bootstrap.Autoconfig;

internal static class ConfigurationExtensions
{
    public static bool HasWavefront(this IConfiguration configuration)
    {
        ArgumentGuard.NotNull(configuration);

        var options = new WavefrontExporterOptions(configuration);
        return !string.IsNullOrEmpty(options.Uri);
    }
}
