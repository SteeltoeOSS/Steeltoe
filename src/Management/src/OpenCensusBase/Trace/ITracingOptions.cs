// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Census.Trace
{
    public interface ITracingOptions
    {
        string Name { get; }

        string IngressIgnorePattern { get; }

        string EgressIgnorePattern { get; }

        int MaxNumberOfAttributes { get; }

        int MaxNumberOfAnnotations { get; }

        int MaxNumberOfMessageEvents { get; }

        int MaxNumberOfLinks { get; }

        bool AlwaysSample { get; }

        bool NeverSample { get;  }

        bool UseShortTraceIds { get; }
    }
}
