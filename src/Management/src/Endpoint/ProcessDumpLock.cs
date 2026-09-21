// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint;

internal static partial class ProcessDumpLock
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public static async Task<IDisposable> EnterAsync(ILogger logger, CancellationToken cancellationToken)
    {
        LogLockEntering(logger);
        await Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        LogLockEntered(logger);

        return new ReleaseOnDispose(logger);
    }

    [LoggerMessage(LogLevel.Trace, "Obtaining exclusive lock")]
    private static partial void LogLockEntering(ILogger logger);

    [LoggerMessage(LogLevel.Trace, "Exclusive lock obtained")]
    private static partial void LogLockEntered(ILogger logger);

    [LoggerMessage(LogLevel.Trace, "Exclusive lock released")]
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
