// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Diagnostics.Listeners;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.EndpointCore.Diagnostics
{
    internal class DiagnosticServices : IHostedService
    {
        private IDiagnosticObserverManager observerManager;
        private ILogger<DiagnosticServices> logger;

        public DiagnosticServices(IDiagnosticObserverManager observerManager, ILogger<DiagnosticServices> logger = null)
        {
            this.observerManager = observerManager;
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            observerManager.Start();
            logger?.LogInformation("Started Diagnostic Observers");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            observerManager.Stop();
            logger?.LogInformation("Stopped Diagnostic Observers");
            return Task.CompletedTask;
        }
    }
}
