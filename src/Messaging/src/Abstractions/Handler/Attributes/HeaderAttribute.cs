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
    /// Attribute which indicates that a method parameter should be bound to a message header.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true)]
    public class HeaderAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HeaderAttribute"/> class.
        /// </summary>
        /// <param name="name">the name of the request header to bind to</param>
        /// <param name="defaultValue">the default value to use as a fallback</param>
        /// <param name="required">is the header required</param>
        public HeaderAttribute(string name = null, string defaultValue = null, bool required = true)
        {
            Name = name ?? string.Empty;
            Required = required;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Gets or sets the name of the header to bind to
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the header binding is required
        /// </summary>
        public virtual bool Required { get; set; }

        /// <summary>
        /// Gets or sets the default value to use if header is missing
        /// </summary>
        public virtual string DefaultValue { get; set; }
    }
}
