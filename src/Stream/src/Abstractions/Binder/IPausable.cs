﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Lifecycle;
using System.Threading.Tasks;

namespace Steeltoe.Stream.Binder
{
    /// <summary>
    /// TODO: IPausable is a integration type.
    /// </summary>
    public interface IPausable : ILifecycle
    {
        Task Pause();

        Task Resume();
    }
}
