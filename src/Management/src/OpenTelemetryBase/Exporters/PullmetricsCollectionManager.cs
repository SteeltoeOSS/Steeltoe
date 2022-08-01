// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry;
using OpenTelemetry.Metrics;
using System.Runtime.CompilerServices;

namespace Steeltoe.Management.OpenTelemetry.Exporters;

// Adapted from OpenTelemetry.Net project
internal sealed partial class PullMetricsCollectionManager
{
    private readonly MetricsExporter _exporter;
    private readonly Func<Batch<Metric>, ExportResult> _onCollectRef;
    private readonly int _scrapeResponseCacheDurationInMilliseconds;

    private int _globalLockState;
    private DateTime? _previousDataViewGeneratedAtUtc;
    private int _readerCount;
    private bool _collectionRunning;
    private TaskCompletionSource<ICollectionResponse> _collectionTcs;

    private ICollectionResponse _previousView;

    public PullMetricsCollectionManager(MetricsExporter exporter)
    {
        this._exporter = exporter;
        _scrapeResponseCacheDurationInMilliseconds = exporter.ScrapeResponseCacheDurationMilliseconds;
        _onCollectRef = OnCollect;
    }

    public Task<ICollectionResponse> EnterCollect()
    {
        EnterGlobalLock();

        // If we are within {ScrapeResponseCacheDurationMilliseconds} of the
        // last successful collect, return the previous view.
        if (_previousDataViewGeneratedAtUtc.HasValue
            && _scrapeResponseCacheDurationInMilliseconds > 0
            && _previousDataViewGeneratedAtUtc.Value.AddMilliseconds(_scrapeResponseCacheDurationInMilliseconds) >= DateTime.UtcNow)
        {
            Interlocked.Increment(ref _readerCount);
            ExitGlobalLock();
            return Task.FromResult(_previousView);
        }

        // If a collection is already running, return a task to wait on the result.
        if (_collectionRunning)
        {
            _collectionTcs ??=
                new TaskCompletionSource<ICollectionResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

            Interlocked.Increment(ref _readerCount);
            ExitGlobalLock();
            return _collectionTcs.Task;
        }

        WaitForReadersToComplete();

        // Start a collection on the current thread.
        _collectionRunning = true;
        _previousDataViewGeneratedAtUtc = null;
        Interlocked.Increment(ref _readerCount);
        ExitGlobalLock();

        ICollectionResponse response;
        bool result = ExecuteCollect();
        if (result)
        {
            _previousDataViewGeneratedAtUtc = DateTime.UtcNow;
            response = _exporter.GetCollectionResponse(_previousView, _previousDataViewGeneratedAtUtc.Value);
        }
        else
        {
            response = default;
        }

        EnterGlobalLock();

        _collectionRunning = false;

        if (_collectionTcs != null)
        {
            _collectionTcs.SetResult(response);
            _collectionTcs = null;
        }

        ExitGlobalLock();

        return Task.FromResult(response);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ExitCollect()
    {
        Interlocked.Decrement(ref _readerCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnterGlobalLock()
    {
        SpinWait lockWait = default;
        while (true)
        {
            if (Interlocked.CompareExchange(ref _globalLockState, 1, _globalLockState) != 0)
            {
                lockWait.SpinOnce();
                continue;
            }

            break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExitGlobalLock()
    {
        _globalLockState = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WaitForReadersToComplete()
    {
        SpinWait readWait = default;
        while (true)
        {
            if (Interlocked.CompareExchange(ref _readerCount, 0, _readerCount) != 0)
            {
                readWait.SpinOnce();
                continue;
            }

            break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool ExecuteCollect()
    {
        _exporter.OnExport = _onCollectRef;
        var result = _exporter.Collect?.Invoke(Timeout.Infinite);
        _exporter.OnExport = null;
        return result ?? false;
    }

    private ExportResult OnCollect(Batch<Metric> metrics)
    {
        try
        {
            _previousView = _exporter.GetCollectionResponse(metrics);
            return ExportResult.Success;
        }
        catch (Exception)
        {
            _previousView = _exporter.GetCollectionResponse();
            return ExportResult.Failure;
        }
    }
}
