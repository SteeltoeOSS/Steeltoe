// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
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
        internal IDisposable _listenersSubscription;
        internal ILogger<DiagnosticsManager> _logger;
        internal IList<IDiagnosticObserver> _observers;
        internal IList<IPolledDiagnosticSource> _sources;
        internal Thread _workerThread;
        internal bool _workerThreadShutdown = false;
        internal int _started = 0;

        private const int POLL_DELAY_MILLI = 15000;

        private static readonly Lazy<DiagnosticsManager> AsSingleton = new Lazy<DiagnosticsManager>(() => new DiagnosticsManager());

        public static DiagnosticsManager Instance => AsSingleton.Value;

        public DiagnosticsManager(IEnumerable<IPolledDiagnosticSource> polledSources, IEnumerable<IDiagnosticObserver> observers, ILogger<DiagnosticsManager> logger = null)
        {
            if (polledSources == null)
            {
                throw new ArgumentNullException(nameof(polledSources));
            }

            if (observers == null)
            {
                throw new ArgumentNullException(nameof(observers));
            }

            this._logger = logger;
            this._observers = observers.ToList();
            this._sources = polledSources.ToList();
        }

        internal DiagnosticsManager(ILogger<DiagnosticsManager> logger = null)
        {
            this._logger = logger;
            this._observers = new List<IDiagnosticObserver>();
            this._sources = new List<IPolledDiagnosticSource>();
        }

        public IList<IDiagnosticObserver> Observers => _observers;

        public IList<IPolledDiagnosticSource> Sources => _sources;

        public void Dispose()
        {
            Stop();

            if (_observers != null)
            {
                _observers.Clear();
            }

            if (_sources != null)
            {
                _sources.Clear();
            }

            _logger = null;
        }

        public void OnCompleted()
        {
            // for future use
        }

        public void OnError(Exception error)
        {
            // for future use
        }

        public void OnNext(DiagnosticListener value)
        {
            foreach (var listener in _observers)
            {
                listener.Subscribe(value);
            }
        }

        public void Start()
        {
            if (Interlocked.CompareExchange(ref _started, 1, 0) == 0)
            {
                this._listenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);

                _workerThread = new Thread(this.Poller)
                {
                    IsBackground = true,
                    Name = "DiagnosticsPoller"
                };
                _workerThread.Start();
            }
        }

        public void Stop()
        {
            if (Interlocked.CompareExchange(ref _started, 0, 1) == 1)
            {
                _workerThreadShutdown = true;

                foreach (var listener in _observers)
                {
                    listener.Dispose();
                }
            }
        }

        private void Poller(object obj)
        {
            while (!_workerThreadShutdown)
            {
                try
                {
                    foreach (var source in _sources)
                    {
                        source.Poll();
                    }

                    Thread.Sleep(POLL_DELAY_MILLI);
                }
                catch (Exception e)
                {
                    _logger?.LogError(e, "Diagnostic source poller exception, terminating");
                    return;
                }
            }
        }
    }
}
