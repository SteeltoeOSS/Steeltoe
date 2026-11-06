// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Extensions;

namespace Steeltoe.Management.Endpoint.SpringBootAdminClient;

internal sealed partial class SpringBootAdminPeriodicRefresh : IAsyncDisposable
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
            LogStartingPeriodicRefreshLoop(interval);
            bool isFirstTime = true;

            do
            {
                LogStartingRefreshCycle();

                try
                {
                    await _runner.RunAsync(isFirstTime, _timerTokenSource.Token);
                }
                catch (Exception exception) when (!exception.IsCancellation())
                {
                    LogRefreshCycleFailed(exception);
                }

                isFirstTime = false;
            }
            while (await _periodicTimer.WaitForNextTickAsync(_timerTokenSource.Token));
        }
        catch (OperationCanceledException)
        {
            LogPeriodicRefreshLoopStopped();
        }
    }

    private void ChangeInterval(TimeSpan interval)
    {
        TimeSpan safeInterval = InfiniteWhenZero(interval);

        if (safeInterval != _periodicTimer.Period)
        {
            _periodicTimer.Period = safeInterval;
            LogRefreshIntervalChanged(safeInterval);
        }
    }

    private static TimeSpan InfiniteWhenZero(TimeSpan interval)
    {
        // Translate disabled to a very high value, so we can resume when configuration changes.
        return interval <= TimeSpan.Zero ? Timeout.InfiniteTimeSpan : interval;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        LogSignalingStop();
        await DisposeAsync();

        LogStartingCleanup();
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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting periodic refresh loop with interval {Interval}.")]
    private partial void LogStartingPeriodicRefreshLoop(TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting refresh cycle.")]
    private partial void LogStartingRefreshCycle();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Refresh cycle failed.")]
    private partial void LogRefreshCycleFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Stopped periodic refresh loop.")]
    private partial void LogPeriodicRefreshLoopStopped();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Refresh interval changed to {Interval}.")]
    private partial void LogRefreshIntervalChanged(TimeSpan interval);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Signaling to stop periodic refresh loop.")]
    private partial void LogSignalingStop();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting cleanup.")]
    private partial void LogStartingCleanup();
}
