// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Management.OpenTelemetry.Exporters;

public readonly struct PrometheusCollectionResponse : ICollectionResponse
{
    public PrometheusCollectionResponse(ArraySegment<byte> view, DateTime generatedAtUtc)
    {
        View = view;
        GeneratedAtUtc = generatedAtUtc;
    }

    public ArraySegment<byte> View { get; }

    public DateTime GeneratedAtUtc { get; }
}
