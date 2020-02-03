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

using System;
using System.Collections.Generic;

namespace Steeltoe.Integration.Retry
{
    public abstract class RetryTemplate : IRetryOperation
    {
        protected List<IRetryListener> listeners = new List<IRetryListener>();

        public void RegisterListener(IRetryListener listener)
        {
            listeners.Add(listener);
        }

        public abstract T Execute<T>(Func<IRetryContext, T> retryCallback);

        public abstract T Execute<T>(Func<IRetryContext, T> retryCallback, IRecoveryCallback<T> recoveryCallback);

        public abstract void Execute(Action<IRetryContext> retryCallback);

        public abstract void Execute(Action<IRetryContext> retryCallback, IRecoveryCallback recoveryCallback);
    }
}
