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
