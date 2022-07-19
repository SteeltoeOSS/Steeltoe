// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Common.Util;

public class TimerReference : IDisposable
{
    internal ITimerListener Listener;
    internal CancellationTokenSource TokenSource;
    internal TimeSpan Period;
    internal Task TimerTask;

    public TimerReference(ITimerListener listener, TimeSpan period)
    {
        this.Listener = listener;
        TokenSource = new CancellationTokenSource();
        this.Period = period;
        TimerTask = new Task(() => { Run(TokenSource); }, TaskCreationOptions.LongRunning);
    }

    public void Start()
    {
        TimerTask.Start();
    }

    public void Run(CancellationTokenSource tokenSource)
    {
        while (!tokenSource.IsCancellationRequested)
        {
            Time.WaitUntil(() => tokenSource.IsCancellationRequested, (int)Period.TotalMilliseconds);

            if (!tokenSource.IsCancellationRequested)
            {
                Listener.Tick();
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!TokenSource.IsCancellationRequested)
            {
                TokenSource.Cancel();
            }

            Listener = null;
            TimerTask = null;
        }
    }
}
