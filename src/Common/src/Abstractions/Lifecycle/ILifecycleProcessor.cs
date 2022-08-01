// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Lifecycle;

/// <summary>
/// Interface for processing lifecycle based services.
/// </summary>
public interface ILifecycleProcessor : IDisposable
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

    /// <summary>
    /// Call to refresh the lifecycle processor.
    /// </summary>
    /// <returns>a task to signal completion.</returns>
    Task OnRefresh();

    /// <summary>
    /// Call to shutdown the lifecycle processor.
    /// </summary>
    /// <returns>a task to signal completion.</returns>
    Task OnClose();
}
