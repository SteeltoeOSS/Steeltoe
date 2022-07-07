// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Trace;

public class TraceEndpointOptions : AbstractEndpointOptions, ITraceOptions
{
    private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:trace";
    private const int DEFAULT_CAPACITY = 100;

    public TraceEndpointOptions()
    {
        Id = "trace";
        Capacity = DEFAULT_CAPACITY;
    }

    public TraceEndpointOptions(IConfiguration config)
        : base(MANAGEMENT_INFO_PREFIX, config)
    {
        if (string.IsNullOrEmpty(Id))
        {
            Id = "trace";
        }

        if (Capacity == -1)
        {
            Capacity = DEFAULT_CAPACITY;
        }
    }

    public int Capacity { get; set; } = -1;

    public bool AddRequestHeaders { get; set; } = true;

    public bool AddResponseHeaders { get; set; } = true;

    public bool AddPathInfo { get; set; }

    public bool AddUserPrincipal { get; set; }

    public bool AddParameters { get; set; }

    public bool AddQueryString { get; set; }

    public bool AddAuthType { get; set; }

    public bool AddRemoteAddress { get; set; }

    public bool AddSessionId { get; set; }

    public bool AddTimeTaken { get; set; } = true;
}
