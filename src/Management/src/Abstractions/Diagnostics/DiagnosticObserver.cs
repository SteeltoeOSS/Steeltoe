// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Steeltoe.Common.Diagnostics
{
    public abstract class DiagnosticObserver : IDiagnosticObserver
    {
        public string ListenerName { get; }

        public string ObserverName { get; }

        protected ILogger Logger { get; }

        protected IDisposable Subscription { get; set; }

        protected DiagnosticObserver(string name, string listenerName, ILogger logger = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(nameof(name));
            }

            if (string.IsNullOrEmpty(listenerName))
            {
                throw new ArgumentException(nameof(listenerName));
            }

            ObserverName = name;
            ListenerName = listenerName;
            Logger = logger;
        }

        public void Dispose()
        {
            if (Subscription != null)
            {
                Subscription.Dispose();
            }

            Subscription = null;
            Logger?.LogInformation("DiagnosticObserver {observer} Disposed", ObserverName);
        }

        public void Subscribe(DiagnosticListener listener)
        {
            if (ListenerName == listener.Name)
            {
                if (Subscription != null)
                {
                    Dispose();
                }

                Subscription = listener.Subscribe(this);
                Logger?.LogInformation("DiagnosticObserver {observer} Subscribed to {listener}", ObserverName, listener.Name);
            }
        }

        public virtual void OnCompleted()
        {
        }

        public virtual void OnError(Exception error)
        {
        }

        public virtual void OnNext(KeyValuePair<string, object> @event)
        {
            try
            {
                ProcessEvent(@event.Key, @event.Value);
            }
            catch (Exception e)
            {
                Logger?.LogError(e, "ProcessEvent exception: {Id}", @event.Key);
            }
        }

        public abstract void ProcessEvent(string @event, object arg);
    }
}
