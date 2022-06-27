// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Steeltoe.Common.Lifecycle;

/// <summary>
/// A common interface defining methods for start/stop lifecycle control.
/// </summary>
public interface ILifecycle
{
    /// <summary>
    /// Start this component.
    /// </summary>
    /// <returns>a task to signal completion.</returns>
    Task Start();

    /// <summary>
    /// Stop this component.
    /// </summary>
    /// <returns>a task to signal completion.</returns>
    Task Stop();

    /// <summary>
    /// Gets a value indicating whether its running.
    /// </summary>
    bool IsRunning { get; }
}
