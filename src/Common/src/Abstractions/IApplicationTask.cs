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

namespace Steeltoe.Common
{
    /// <summary>
    /// A runnable task bundled with the assembly that can be executed on-demand
    /// </summary>
    public interface IApplicationTask
    {
        /// <summary>
        /// Gets globally unique name for the task
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Action which to run
        /// </summary>
        void Run();
    }
}