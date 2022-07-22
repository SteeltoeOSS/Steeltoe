// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka.Test;

public class MyHealthCheckHandler : IHealthCheckHandler
{
    private readonly InstanceStatus _status;

    public bool Called { get; set; }

    public MyHealthCheckHandler(InstanceStatus status)
    {
        _status = status;
        Called = false;
    }

    public InstanceStatus GetStatus(InstanceStatus currentStatus)
    {
        Called = true;
        return _status;
    }
}