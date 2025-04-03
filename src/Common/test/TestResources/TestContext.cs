// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#pragma warning disable IDE0130 // Namespace does not match folder structure

// ReSharper disable once CheckNamespace
namespace Xunit;

/// <summary>
/// This temporary stub exists to ease the migration to xUnit v3.
/// </summary>
public sealed class TestContext
{
    public static TestContext Current { get; } = new();

    public CancellationToken CancellationToken { get; } = CancellationToken.None;

    private TestContext()
    {
    }
}
