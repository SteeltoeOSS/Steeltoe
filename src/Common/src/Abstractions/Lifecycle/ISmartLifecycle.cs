// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Steeltoe.Common.Lifecycle
{
    /// <summary>
    /// An extension of the Lifecycle interface that provides auto startup and a call back
    /// stop action
    /// </summary>
    public interface ISmartLifecycle : ILifecycle, IPhased
    {
        /// <summary>
        /// Gets a value indicating whether the auto startup is set for this object
        /// </summary>
        bool IsAutoStartup { get; }

        /// <summary>
        /// Stop the component and issue the callback when complete
        /// </summary>
        /// <param name="callback">the callback action to invoke when complete</param>
        /// <returns>a task for completion</returns>
        Task Stop(Action callback);
    }
}
