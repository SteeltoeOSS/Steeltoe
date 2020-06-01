// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Diagnostics
{
    public class DiagnosticServices : IHostedService
    {
        private readonly IDiagnosticsManager observerManager;
        private readonly ILogger<DiagnosticServices> logger;

        public DiagnosticServices(IDiagnosticsManager observerManager, ILogger<DiagnosticServices> logger = null)
        {
            this.observerManager = observerManager;
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            observerManager.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            observerManager.Stop();
            return Task.CompletedTask;
        }
    }
}
