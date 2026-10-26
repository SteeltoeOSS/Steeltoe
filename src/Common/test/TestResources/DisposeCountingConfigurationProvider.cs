// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Common.TestResources;

/// <summary>
/// A configuration provider that counts how many times it is disposed, and that can raise a reload on demand.
/// </summary>
public sealed class DisposeCountingConfigurationProvider : ConfigurationProvider, IDisposable
{
    private long _disposeCount;

    /// <summary>
    /// Gets the number of times <see cref="Dispose" /> was called.
    /// </summary>
    public long DisposeCount => Interlocked.Read(ref _disposeCount);

    /// <summary>
    /// Raises the reload change token, as if the underlying data changed.
    /// </summary>
    public void TriggerReload()
    {
        OnReload();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Interlocked.Increment(ref _disposeCount);
    }
}
