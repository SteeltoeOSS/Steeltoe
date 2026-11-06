// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint;

/// <summary>
/// Ensures that heap, GC and thread dumps don't run concurrently, because that may lock up the entire process.
/// </summary>
internal static partial class MemoryDumpLock
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public static IDisposable? TryEnter(ILogger logger)
    {
        if (Semaphore.Wait(TimeSpan.Zero))
        {
            LogLockEntered(logger);
            return new ReleaseOnDispose(logger);
        }

        LogLockBusy(logger);
        return null;
    }

    [LoggerMessage(LogLevel.Trace, "Exclusive lock obtained.")]
    private static partial void LogLockEntered(ILogger logger);

    [LoggerMessage(LogLevel.Trace, "Exclusive lock rejected.")]
    private static partial void LogLockBusy(ILogger logger);

    [LoggerMessage(LogLevel.Trace, "Exclusive lock released.")]
    private static partial void LogLockReleased(ILogger logger);

    private sealed class ReleaseOnDispose(ILogger logger) : IDisposable
    {
        private readonly ILogger _logger = logger;
        private bool _isReleased;

        public void Dispose()
        {
            if (!_isReleased)
            {
                _isReleased = true;
                Semaphore.Release();
                LogLockReleased(_logger);
            }
        }
    }
}
