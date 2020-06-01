// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Exporter.Tracing.Zipkin
{
    public interface ITraceExporterOptions
    {
        string Endpoint { get; }

        bool ValidateCertificates { get; }

        int TimeoutSeconds { get; }

        string ServiceName { get; }

        bool UseShortTraceIds { get; }
    }
}
