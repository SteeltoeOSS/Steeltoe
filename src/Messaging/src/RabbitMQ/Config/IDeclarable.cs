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

namespace Steeltoe.Messaging.Rabbit.Config
{
    public interface IDeclarable
    {
        /// <summary>
        /// Gets a value indicating whether this object should be declared
        /// </summary>
        bool Declare { get; }

        /// <summary>
        /// Gets a collection of Admins that should declare this object
        /// </summary>
        List<object> Admins { get; }

        /// <summary>
        /// Gets a value indicating whether should ignore exceptions
        /// </summary>
        public bool IgnoreDeclarationExceptions { get; }

        /// <summary>
        /// Adds an argument to the declarable
        /// </summary>
        /// <param name="name">the argument name</param>
        /// <param name="value">the argument value</param>
        void AddArgument(string name, object value);

        /// <summary>
        /// Remove an argument from the declarable
        /// </summary>
        /// <param name="name">the argument name</param>
        /// <returns>the value if present</returns>
        object RemoveArgument(string name);
    }
}
