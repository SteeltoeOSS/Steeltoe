// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

/// <summary>
/// An object that can be invoked synchronously via a run method.
/// </summary>
public interface IRunnable
{
    /// <summary>
    /// Run this object
    /// </summary>
    /// <returns>returns success or failure</returns>
    bool Run();
}