// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Steeltoe.Stream.Binder
{
    /// <summary>
    /// A factory for creating or obtaining binders.
    /// </summary>
    /// TODO: Figure out Disposable/Closable/DisposableBean usages
    public interface IBinderFactory // : IDisposable
    {
        /// <summary>
        /// Returns the binder instance associated with the given configuration name. Instance
        /// caching is a requirement, and implementations must return the same instance on
        /// subsequent invocations with the same arguments.
        /// </summary>
        /// <param name="name">the name of the binder in configuration</param>
        /// <returns>the binder</returns>
        IBinder GetBinder(string name);

        /// <summary>
        /// Returns the binder instance associated with the given configuration name. Instance
        /// caching is a requirement, and implementations must return the same instance on
        /// subsequent invocations with the same arguments.
        /// </summary>
        /// <param name="name">the name of the binder in configuration</param>
        /// <param name="bindableType">the binding target type</param>
        /// <returns>the binder</returns>
        IBinder GetBinder(string name, Type bindableType);
    }
}
