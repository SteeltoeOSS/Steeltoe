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
    /// Annotation that marks a method to be a listener to inputs declared via EnableBinding
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class StreamListenerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StreamListenerAttribute"/> class.
        /// </summary>
        /// <param name="target">the name of the binding target (e.g. channel)</param>
        /// <param name="copyHeaders">when true, copy incoming headers to any outgoing messages</param>
        public StreamListenerAttribute(string target, bool copyHeaders = true)
            : this(target, null, copyHeaders)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamListenerAttribute"/> class.
        /// </summary>
        /// <param name="target">the name of the binding target (e.g. channel)</param>
        /// <param name="condition">expression language condition that must be met by all items dispatched to this method</param>
        /// <param name="copyHeaders">when true, copy incoming headers to any outgoing messages</param>
        public StreamListenerAttribute(string target, string condition, bool copyHeaders = true)
        {
            Target = target;
            Condition = condition;
            CopyHeaders = copyHeaders;
        }

        /// <summary>
        /// Gets or sets the binding target (e.g. channel)
        /// </summary>
        public virtual string Target { get; set; }

        /// <summary>
        /// Gets or sets the expression language condition that must be met by all items dispatched to this method
        /// </summary>
        public virtual string Condition { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to copy incoming headers to outgoing messages
        /// </summary>
        public virtual bool CopyHeaders { get; set; }
    }
}
