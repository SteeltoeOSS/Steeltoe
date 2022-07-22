// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using System;
using System.Threading;

namespace Steeltoe.Discovery.Client.Test;

public class TestApplicationLifetime : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted => throw new NotImplementedException();

    public CancellationToken ApplicationStopping => new CancellationTokenSource().Token;

    public CancellationToken ApplicationStopped => throw new NotImplementedException();

    public void StopApplication()
    {
        throw new NotImplementedException();
    }
}