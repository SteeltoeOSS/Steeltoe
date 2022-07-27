// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Retry;

public abstract class RetryTemplate : IRetryOperation
{
    protected List<IRetryListener> listeners = new ();

    public void RegisterListener(IRetryListener listener)
    {
        listeners.Add(listener);
    }

    public abstract T Execute<T>(Func<IRetryContext, T> retryCallback);

    public abstract T Execute<T>(Func<IRetryContext, T> retryCallback, IRecoveryCallback<T> recoveryCallback);

    public abstract void Execute(Action<IRetryContext> retryCallback);

    public abstract void Execute(Action<IRetryContext> retryCallback, IRecoveryCallback recoveryCallback);

    public abstract T Execute<T>(Func<IRetryContext, T> retryCallback, Func<IRetryContext, T> recoveryCallback);

    public abstract void Execute(Action<IRetryContext> retryCallback, Action<IRetryContext> recoveryCallback);
}