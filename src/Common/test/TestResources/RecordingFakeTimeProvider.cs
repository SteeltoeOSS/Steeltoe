// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Time.Testing;

// Justification: SemaphoreSlim only allocates a wait handle when AvailableWaitHandle is accessed, which is never done here.
#pragma warning disable CA1001 // Types that own disposable fields should be disposable

namespace Steeltoe.Common.TestResources;

/// <summary>
/// A <see cref="FakeTimeProvider" /> that also records the due time of each timer that is created, such as the timers behind
/// <see cref="Task.Delay(TimeSpan, TimeProvider, CancellationToken)" />. This makes it possible to assert on the delays that code asks for, and to wait
/// until the code under test has started a delay before advancing the clock by exactly that delay.
/// </summary>
public sealed class RecordingFakeTimeProvider : FakeTimeProvider
{
    private readonly SemaphoreSlim _timerCreated = new(0);
    private readonly List<TimeSpan> _dueTimes = [];
    private int _nextDueTimeIndex;

    /// <summary>
    /// Gets the due times of all timers created so far, in order of creation.
    /// </summary>
    public IReadOnlyList<TimeSpan> DueTimes
    {
        get
        {
            lock (_dueTimes)
            {
                return [.. _dueTimes];
            }
        }
    }

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        lock (_dueTimes)
        {
            _dueTimes.Add(dueTime);
        }

        _timerCreated.Release();

        return base.CreateTimer(callback, state, dueTime, period);
    }

    /// <summary>
    /// Waits until a timer has been created that this method did not return yet, and returns its due time. The timeout is only a safety net against hanging
    /// forever.
    /// </summary>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public async Task<TimeSpan> WaitForNextTimerAsync(CancellationToken cancellationToken)
    {
        if (!await _timerCreated.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
        {
            throw new TimeoutException("Timed out waiting for a timer to be created.");
        }

        lock (_dueTimes)
        {
            return _dueTimes[_nextDueTimeIndex++];
        }
    }
}
