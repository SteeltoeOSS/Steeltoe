// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.TestResources;

/// <summary>
/// Implements <see cref="IDisposable" /> without doing anything.
/// </summary>
public sealed class EmptyDisposable : IDisposable
{
    public static IDisposable Instance { get; } = new EmptyDisposable();

    private EmptyDisposable()
    {
    }

    public void Dispose()
    {
        // Intentionally left empty.
    }
}
