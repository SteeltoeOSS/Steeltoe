﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Steeltoe.Common.Util
{
    /// <summary>
    /// An object that can be invoked asynchronously via a run method.
    /// </summary>
    public interface IAsyncRunnable
    {
        /// <summary>
        /// Run this component.
        /// </summary>
        /// <returns>return a task to signal completion</returns>
        Task<bool> Run();
    }
}
