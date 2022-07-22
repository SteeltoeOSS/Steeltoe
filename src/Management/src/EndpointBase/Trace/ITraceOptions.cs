// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint.Trace;

public interface ITraceOptions : IEndpointOptions
{
    int Capacity { get; }

    bool AddRequestHeaders { get; }

    bool AddResponseHeaders { get; }

    bool AddPathInfo { get; }

    bool AddUserPrincipal { get; }

    bool AddParameters { get; }

    bool AddQueryString { get; }

    bool AddAuthType { get; }

    bool AddRemoteAddress { get; }

    bool AddSessionId { get; }

    bool AddTimeTaken { get; }
}