// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#pragma warning disable CA1724 // Type names should not match namespaces

namespace Steeltoe.Common.TestResources;

/// <summary>
/// Time limits for tests.
/// </summary>
public static class Timeouts
{
    /// <summary>
    /// The longest a test waits for something that normally completes quickly, such as a signal from a background thread. It only exists to make a
    /// regression fail instead of hang, so a test never verifies behavior based on it.
    /// </summary>
    public static readonly TimeSpan Safety = TimeSpan.FromSeconds(30);
}
