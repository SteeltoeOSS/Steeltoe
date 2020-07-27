// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.Tracing
{
    internal class TracingService : IHostedService
    {
        private readonly IDiagnosticsManager _observerManager;
        private readonly ILogger<TracingService> _logger;

        public TracingService(IDiagnosticsManager observerManager, ILogger<TracingService> logger = null)
        {
            _observerManager = observerManager;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _observerManager.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _observerManager.Stop();
            return Task.CompletedTask;
        }
    }
}
