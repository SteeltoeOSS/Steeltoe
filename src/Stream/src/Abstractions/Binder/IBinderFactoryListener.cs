// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using System;

namespace Steeltoe.Stream.Binder
{
    /// <summary>
    /// A listener that can be registered with the BinderFactory that allows the registration of additional configuration.
    /// </summary>
    public interface IBinderFactoryListener
    {
        /// <summary>
        /// Applying additional capabilities to the binder after the binder context has been initialized.
        /// </summary>
        /// <param name="configurationName">the binders configuration name</param>
        /// <param name="binderContext">the configured service provider</param>
        void AfterBinderInitialized(string configurationName, IApplicationContext binderContext);
    }
}
