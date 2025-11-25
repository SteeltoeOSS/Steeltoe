// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Extensions;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

internal sealed class SpringBootAdminPeriodicRefresh : IAsyncDisposable
{
    private readonly SpringBootAdminRefreshRunner _runner;
    private readonly ILogger<SpringBootAdminPeriodicRefresh> _logger;
    private readonly PeriodicTimer _periodicTimer;
    private readonly CancellationTokenSource _timerTokenSource = new();
    private readonly Task _timerTask;
    private readonly IDisposable? _optionsChangeToken;
    private bool _isDisposed;

    public SpringBootAdminPeriodicRefresh(IOptionsMonitor<SpringBootAdminClientOptions> optionsMonitor, TimeProvider timeProvider,
        SpringBootAdminRefreshRunner runner, ILogger<SpringBootAdminPeriodicRefresh> logger)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(logger);

        TimeSpan safeInterval = InfiniteWhenZero(optionsMonitor.CurrentValue.RefreshInterval);

        _runner = runner;
        _logger = logger;
        _periodicTimer = new PeriodicTimer(safeInterval, timeProvider);
        _timerTask = TimerLoopAsync(safeInterval);
        _optionsChangeToken = optionsMonitor.OnChange(options => ChangeInterval(options.RefreshInterval));
    }

    private async Task TimerLoopAsync(TimeSpan interval)
    {
        try
        {
            _logger.LogDebug("Starting periodic refresh loop with interval {Interval}.", interval);
            bool isFirstTime = true;

            do
            {
                _logger.LogDebug("Starting refresh cycle.");

                try
                {
                    await _runner.RunAsync(isFirstTime, _timerTokenSource.Token);
                }
                catch (Exception exception) when (!exception.IsCancellation())
                {
                    _logger.LogWarning(exception, "Refresh cycle failed.");
                }

                isFirstTime = false;
            }
            while (await _periodicTimer.WaitForNextTickAsync(_timerTokenSource.Token));
        }
        catch (OperationCanceledException)
        {
#pragma warning disable S6667 // Logging in a catch clause should pass the caught exception as a parameter.
            // Justification: The exception contains no useful information. Logging it suggests something crashed, while this is expected behavior.
            _logger.LogDebug("Stopped periodic refresh loop.");
#pragma warning restore S6667 // Logging in a catch clause should pass the caught exception as a parameter.
        }
    }

    private void ChangeInterval(TimeSpan interval)
    {
        TimeSpan safeInterval = InfiniteWhenZero(interval);

        if (safeInterval != _periodicTimer.Period)
        {
            _periodicTimer.Period = safeInterval;
            _logger.LogDebug("Refresh interval changed to {Interval}.", safeInterval);
        }
    }

    private static TimeSpan InfiniteWhenZero(TimeSpan interval)
    {
        // Translate disabled to a very high value, so we can resume when configuration changes.
        return interval <= TimeSpan.Zero ? Timeout.InfiniteTimeSpan : interval;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Signaling to stop periodic refresh loop.");
        await DisposeAsync();

        _logger.LogDebug("Starting cleanup.");
        await _runner.CleanupAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_isDisposed)
        {
            _isDisposed = true;
            _optionsChangeToken?.Dispose();
            await _timerTokenSource.CancelAsync();
            await _timerTask;
            _timerTokenSource.Dispose();
            _timerTask.Dispose();
            _periodicTimer.Dispose();
        }
    }
}
