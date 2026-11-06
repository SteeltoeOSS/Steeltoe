// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Configuration.ConfigServer;

/// <summary>
/// Allows only one unit of work to run at a time, and lets the owner wait for the work that is running when it shuts down. This is the only thing that
/// disposal of the owner waits for, so the work in the gate must never depend on code that the owner does not control, such as listeners of change
/// notifications.
/// </summary>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
// Justification: SemaphoreSlim only allocates a wait handle when AvailableWaitHandle is accessed, which is never done here.
internal sealed class WorkGate
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <summary>
    /// Enters the gate if no work is running, without waiting.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the gate was entered, which must be followed by <see cref="Exit" />; <c>false</c> if other work is running, or the gate is closed.
    /// </returns>
    public bool TryEnter()
    {
        return _semaphore.Wait(0);
    }

    /// <summary>
    /// Enters the gate, waiting for the work that is running to complete. This must be followed by <see cref="Exit" />.
    /// </summary>
    /// <param name="cancellationToken">
    /// Stops waiting. The owner must cancel it before calling <see cref="CloseAndWait" />, because waiting on a closed gate never ends otherwise.
    /// </param>
    public void Enter(CancellationToken cancellationToken)
    {
        _semaphore.Wait(cancellationToken);
    }

    /// <summary>
    /// Leaves the gate, after a call to <see cref="TryEnter" /> or <see cref="Enter" /> that entered it.
    /// </summary>
    public void Exit()
    {
        _semaphore.Release();
    }

    /// <summary>
    /// Waits for the work that is running to complete, then closes the gate for good: nothing can enter it anymore. Call this only once.
    /// </summary>
    public void CloseAndWait()
    {
        _semaphore.Wait();
    }
}
