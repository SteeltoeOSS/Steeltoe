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

namespace Steeltoe.Messaging.Handler.Attributes
{
    /// <summary>
    /// Attribute that indicates a method parameter should be bound to a template variable
    /// in a destination template string. Supported on message handling methods such as
    /// those attributed with MessageMapping
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class DestinationVariableAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DestinationVariableAttribute"/> class.
        /// </summary>
        /// <param name="name">the name of the destination template variable</param>
        public DestinationVariableAttribute(string name = null)
        {
            Name = name ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the name of the destination template variable
        /// </summary>
        public virtual string Name { get; set; }
    }
}
