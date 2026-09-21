// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Management.Endpoint;

internal static class ProcessDumpLock
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    public static async Task<IDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        await Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        return new ReleaseOnDispose();
    }

    private sealed class ReleaseOnDispose : IDisposable
    {
        private bool _isReleased;

        public void Dispose()
        {
            if (!_isReleased)
            {
                _isReleased = true;
                Semaphore.Release();
            }
        }
    }
}
