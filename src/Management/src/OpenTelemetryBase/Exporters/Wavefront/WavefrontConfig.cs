// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.OpenTelemetry.Exporters
{
    public class WavefrontConfig
    {
        public string WavefrontURL { get; internal set; }

        public string Token { get; internal set; }

        public int MaxQueueSize { get; internal set; }

        public int BatchSize { get; internal set; }

        public int Step { get; internal set; } // Milliseconds

        public string Source { get; internal set; }

        public string AppName { get; internal set; }

        public string Service { get; internal set; }

        public string Cluster { get; internal set; }
    }
}