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
    /// A listener that can be registered with the BinderFactory that allows the registration of additional configuration.
    /// </summary>
    public interface IBinderFactoryListener
    {
        /// <summary>
        /// Applying additional capabilities to the binder after the binder context has been initialized.
        /// </summary>
        /// <param name="configurationName">the binders configuration name</param>
        /// <param name="binderContext">the configured service provider</param>
        void AfterBinderInitialized(string configurationName, IServiceProvider binderContext);
    }
}
