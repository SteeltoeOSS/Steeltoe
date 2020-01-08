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

namespace Steeltoe.Common.Diagnostics
{
    public abstract class DiagnosticObserver : IDiagnosticObserver
    {
        public string ListenerName { get; }

        public string ObserverName { get; }

        protected ILogger Logger { get; }

        protected IDisposable Subscription { get; set; }

        protected DiagnosticObserver(string name, string listenerName,  ILogger logger = null)
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
