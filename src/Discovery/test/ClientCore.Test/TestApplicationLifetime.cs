// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NETCOREAPP3_1 || NET5_0
using Microsoft.Extensions.Hosting;
#else
using Microsoft.AspNetCore.Hosting;
#endif
using System;
using System.Threading;

namespace Steeltoe.Discovery.Client.Test
{
#if NETCOREAPP3_1 || NET5_0
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
#else
    public class TestApplicationLifetime : IApplicationLifetime
    {
        public CancellationToken ApplicationStarted => throw new NotImplementedException();

        public CancellationToken ApplicationStopping => new CancellationTokenSource().Token;

        public CancellationToken ApplicationStopped => throw new NotImplementedException();

        public void StopApplication()
        {
            throw new NotImplementedException();
        }
    }
#endif
}
