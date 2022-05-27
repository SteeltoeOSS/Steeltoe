// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using OpenTelemetry;
using OpenTelemetry.Metrics;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.OpenTelemetry.Exporters
{
#pragma warning disable SX1309 // Field names should begin with underscore

    // Adapted from OpenTelemetry.Net project
    internal sealed partial class PullmetricsCollectionManager
    {
        private readonly IMetricsExporter exporter;
        private readonly Func<Batch<Metric>, ExportResult> onCollectRef;
        private readonly int scrapeResponseCacheDurationInMilliseconds;

        private int globalLockState;
        private DateTime? previousDataViewGeneratedAtUtc;
        private int readerCount;
        private bool collectionRunning;
        private TaskCompletionSource<ICollectionResponse> collectionTcs;

        private ICollectionResponse previousView;

#pragma warning restore SX1309 // Field names should begin with underscore
        public PullmetricsCollectionManager(IMetricsExporter exporter)
        {
            this.exporter = exporter;
            this.scrapeResponseCacheDurationInMilliseconds = exporter.ScrapeResponseCacheDurationMilliseconds;
            this.onCollectRef = this.OnCollect;
        }

#if NETCOREAPP3_1_OR_GREATER
        public ValueTask<ICollectionResponse> EnterCollect()
#else
        public Task<ICollectionResponse> EnterCollect()
#endif
        {
            this.EnterGlobalLock();

            // If we are within {ScrapeResponseCacheDurationMilliseconds} of the
            // last successful collect, return the previous view.
            if (this.previousDataViewGeneratedAtUtc.HasValue
                && this.scrapeResponseCacheDurationInMilliseconds > 0
                && this.previousDataViewGeneratedAtUtc.Value.AddMilliseconds(this.scrapeResponseCacheDurationInMilliseconds) >= DateTime.UtcNow)
            {
                Interlocked.Increment(ref this.readerCount);
                this.ExitGlobalLock();
#if NETCOREAPP3_1_OR_GREATER
                return new ValueTask<ICollectionResponse>(previousView);
#else
                return Task.FromResult(previousView);
#endif
            }

            // If a collection is already running, return a task to wait on the result.
            if (this.collectionRunning)
            {
                this.collectionTcs ??=
                    new TaskCompletionSource<ICollectionResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

                Interlocked.Increment(ref this.readerCount);
                this.ExitGlobalLock();
#if NETCOREAPP3_1_OR_GREATER
                return new ValueTask<ICollectionResponse>(this.collectionTcs.Task);
#else
                return this.collectionTcs.Task;
#endif
            }

            this.WaitForReadersToComplete();

            // Start a collection on the current thread.
            this.collectionRunning = true;
            this.previousDataViewGeneratedAtUtc = null;
            Interlocked.Increment(ref this.readerCount);
            this.ExitGlobalLock();

            ICollectionResponse response;
            bool result = this.ExecuteCollect();
            if (result)
            {
                this.previousDataViewGeneratedAtUtc = DateTime.UtcNow;
                response = exporter.GetCollectionResponse(previousView, previousDataViewGeneratedAtUtc.Value);
            }
            else
            {
                response = default;
            }

            this.EnterGlobalLock();

            this.collectionRunning = false;

            if (this.collectionTcs != null)
            {
                this.collectionTcs.SetResult(response);
                this.collectionTcs = null;
            }

            this.ExitGlobalLock();

#if NETCOREAPP3_1_OR_GREATER
            return new ValueTask<ICollectionResponse>(response);
#else
            return Task.FromResult(response);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExitCollect()
        {
            Interlocked.Decrement(ref this.readerCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnterGlobalLock()
        {
            SpinWait lockWait = default;
            while (true)
            {
                if (Interlocked.CompareExchange(ref this.globalLockState, 1, this.globalLockState) != 0)
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
            this.globalLockState = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WaitForReadersToComplete()
        {
            SpinWait readWait = default;
            while (true)
            {
                if (Interlocked.CompareExchange(ref this.readerCount, 0, this.readerCount) != 0)
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
            this.exporter.OnExport = this.onCollectRef;
            var result = this.exporter.Collect?.Invoke(Timeout.Infinite);
            this.exporter.OnExport = null;
            return result ?? false;
        }

        private ExportResult OnCollect(Batch<Metric> metrics)
        {
            try
            {
                previousView = exporter.GetCollectionResponse(metrics);
                return ExportResult.Success;
            }
            catch (Exception)
            {
                previousView = exporter.GetCollectionResponse();
                return ExportResult.Failure;
            }
        }
    }
}
