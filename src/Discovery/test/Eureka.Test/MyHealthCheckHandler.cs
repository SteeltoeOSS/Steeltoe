// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka.Test;

public sealed class MyHealthCheckHandler : IHealthCheckHandler
{
    private readonly InstanceStatus _status;

    public bool Awaited { get; set; }

    public MyHealthCheckHandler(InstanceStatus status)
    {
        _status = status;
        Awaited = false;
    }

    public async Task<InstanceStatus> GetStatusAsync(InstanceStatus currentStatus, CancellationToken cancellationToken)
    {
        await Task.Yield();

        Awaited = true;
        return _status;
    }
}
