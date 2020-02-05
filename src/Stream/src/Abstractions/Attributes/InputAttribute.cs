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

namespace Steeltoe.Stream.Attributes
{
    /// <summary>
    /// Indicates that an input binding target will be created by the framework.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class InputAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputAttribute"/> class.
        /// </summary>
        public InputAttribute()
        {
            Name = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputAttribute"/> class.
        /// </summary>
        /// <param name="name">the binding target name</param>
        public InputAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Gets or sets the binding target name; used as a name for binding target and as a destination name by default.
        /// </summary>
        public virtual string Name { get; set; }
    }
}
