// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

            _logger = logger;
            _observers = observers.ToList();
            _sources = polledSources.ToList();
        }

        internal DiagnosticsManager(ILogger<DiagnosticsManager> logger = null)
        {
            _logger = logger;
            _observers = new List<IDiagnosticObserver>();
            _sources = new List<IPolledDiagnosticSource>();
        }

        public IList<IDiagnosticObserver> Observers => _observers;

        public IList<IPolledDiagnosticSource> Sources => _sources;

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
                _listenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);

                _workerThread = new Thread(Poller)
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

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Cleanup
            if (!_disposed)
            {
                if (disposing)
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

                _disposed = true;
            }
        }

        ~DiagnosticsManager()
        {
            Dispose(false);
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
