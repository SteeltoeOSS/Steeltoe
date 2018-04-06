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

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Steeltoe.Common.Diagnostics
{
    public class DiagnosticsManager : IObserver<DiagnosticListener>, IDisposable, IDiagnosticsManager
    {
        private const int POLL_DELAY_MILLI = 15000;

        private IDisposable listenersSubscription;
        private ILogger<DiagnosticsManager> logger;
        private IList<IDiagnosticObserver> observers;
        private IList<IPolledDiagnosticSource> sources;
        private Thread workerThread;
        private bool workerThradShutdown = false;

        public DiagnosticsManager(IEnumerable<IPolledDiagnosticSource> polledSources, IEnumerable<IDiagnosticObserver> observers, ILogger<DiagnosticsManager> logger = null)
        {
            if (observers == null)
            {
                throw new ArgumentNullException(nameof(observers));
            }

            this.logger = logger;
            this.observers = observers.ToList();
            this.sources = polledSources.ToList();
        }

        public void Dispose()
        {
            Stop();
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener value)
        {
            foreach (var listener in observers)
            {
                listener.Subscribe(value);
            }
        }

        public void Start()
        {
            this.listenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);

            workerThread = new Thread(this.Poller)
            {
                IsBackground = true,
                Name = "DiagnosticsPoller"
            };
            workerThread.Start();
        }

        public void Stop()
        {
            workerThradShutdown = true;

            foreach (var listener in observers)
            {
                listener.Dispose();
            }

            observers.Clear();
        }

        private void Poller(object obj)
        {
            while (!workerThradShutdown)
            {
                try
                {
                    foreach (var source in this.sources)
                    {
                        source.Poll();
                    }

                    Thread.Sleep(POLL_DELAY_MILLI);
                }
                catch (Exception e)
                {
                    logger?.LogError(e, "Metrics poller exception, terminating");
                    return;
                }
            }
        }
    }
}
