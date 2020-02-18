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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Stream.Binder
{
    /// <summary>
    /// Represents a binding between an input or output and an adapter endpoint that connects
    /// via a Binder.The binding could be for a consumer or a producer. A consumer binding
    /// represents a connection from an adapter to an input. A producer binding represents a
    /// connection from an output to an adapter.
    /// </summary>
    public interface IBinding : IPausable
    {
        /// <summary>
        /// Gets the extended info associated with the binding
        /// </summary>
        IDictionary<string, object> ExtendedInfo { get; }

        /// <summary>
        /// Gets the name of the destination for this binding.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the name of the target for this binding (i.e., channel name).
        /// </summary>
        string BindingName { get; }

        /// <summary>
        /// Gets a value indicating whether this binding is an input binding
        /// </summary>
        bool IsInput { get; }

        /// <summary>
        /// Unbinds the target component represented by this instance and stops any active components
        /// </summary>
        /// <returns>task to signal results</returns>
        Task Unbind();
    }
}
