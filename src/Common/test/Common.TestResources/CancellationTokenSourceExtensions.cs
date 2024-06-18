// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if NET6_0
// ReSharper disable once CheckNamespace
namespace System.Threading;

public static class CancellationTokenSourceExtensions
{
    // Workaround for Sonar S6966 bug: CancelAsync is suggested, which isn't available in .NET 6.
    public static Task CancelAsync(this CancellationTokenSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        source.Cancel();
        return Task.CompletedTask;
    }
}
#endif
