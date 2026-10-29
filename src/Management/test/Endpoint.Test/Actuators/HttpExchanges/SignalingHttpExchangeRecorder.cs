// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Endpoint.Actuators.HttpExchanges;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges.Diagnostics;

// Justification: SemaphoreSlim only allocates a wait handle when AvailableWaitHandle is accessed, which is never done here.
#pragma warning disable CA1001 // Types that own disposable fields should be disposable

namespace Steeltoe.Management.Endpoint.Test.Actuators.HttpExchanges;

/// <summary>
/// Wraps the real recorder, so tests can wait until exchanges have been handed to the repository, instead of guessing how long that takes.
/// </summary>
internal sealed class SignalingHttpExchangeRecorder(IEnumerable<DiagnosticObserver> observers) : IHttpExchangeRecorder
{
    private readonly DiagnosticObserverHttpExchangeRecorder _recorder = observers.OfType<DiagnosticObserverHttpExchangeRecorder>().Single();
    private readonly SemaphoreSlim _recorded = new(0);

    public void HandleRecording(Action<HttpExchange> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _recorder.HandleRecording(exchange =>
        {
            handler(exchange);
            _recorded.Release();
        });
    }

    /// <summary>
    /// Waits until the specified number of exchanges has been recorded since the last call. The timeout is only a safety net against hanging forever.
    /// </summary>
    /// <param name="count">
    /// The number of exchanges to wait for.
    /// </param>
    /// <param name="cancellationToken">
    /// The token to monitor for cancellation requests.
    /// </param>
    public async Task WaitForExchangesAsync(int count, CancellationToken cancellationToken)
    {
        for (int index = 0; index < count; index++)
        {
            if (!await _recorded.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken))
            {
                throw new TimeoutException($"Timed out waiting for {count} HTTP exchange(s) to be recorded.");
            }
        }
    }
}
