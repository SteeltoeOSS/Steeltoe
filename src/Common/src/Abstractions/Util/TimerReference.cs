// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Common.Util;

public class TimerReference : IDisposable
{
    internal ITimerListener _listener;
    internal CancellationTokenSource _tokenSource;
    internal TimeSpan _period;
    internal Task _timerTask;

    public TimerReference(ITimerListener listener, TimeSpan period)
    {
        _listener = listener;
        _tokenSource = new CancellationTokenSource();
        _period = period;
        _timerTask = new Task(() => { Run(_tokenSource); }, TaskCreationOptions.LongRunning);
    }

    public void Start()
    {
        _timerTask.Start();
    }

    public void Run(CancellationTokenSource tokenSource)
    {
        while (!tokenSource.IsCancellationRequested)
        {
            Time.WaitUntil(() => tokenSource.IsCancellationRequested, (int)_period.TotalMilliseconds);

            if (!tokenSource.IsCancellationRequested)
            {
                _listener.Tick();
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
            if (!_tokenSource.IsCancellationRequested)
            {
                _tokenSource.Cancel();
            }

            _listener = null;
            _timerTask = null;
        }
    }
}
