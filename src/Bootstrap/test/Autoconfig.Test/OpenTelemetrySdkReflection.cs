// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry.Trace;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Bootstrap.Autoconfig.Test;

/// <summary>
/// Reflection helpers for OpenTelemetry SDK internals used by tests.
/// </summary>
public static class OpenTelemetrySdkReflection
{
    public static List<object> GetTracerProviderInstrumentations(TracerProvider tracerProvider)
    {
        var property = tracerProvider?.GetType().GetProperty(
            "Instrumentations",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        return property?.GetValue(tracerProvider) as List<object>;
    }
}
