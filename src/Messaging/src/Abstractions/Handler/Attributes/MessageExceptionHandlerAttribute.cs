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
    ///  Attribute for handling exceptions thrown from message-handling methods within a specific handler class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MessageExceptionHandlerAttribute : Attribute
    {
        private readonly Type[] exceptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageExceptionHandlerAttribute"/> class.
        /// </summary>
        /// <param name="exceptions">the exceptions handled by this method</param>
        public MessageExceptionHandlerAttribute(params Type[] exceptions)
        {
            this.exceptions = exceptions;
        }

        /// <summary>
        /// Gets the exceptions handled by this method
        /// </summary>
        public virtual Type[] Exceptions
        {
            get { return exceptions; }
        }
    }
}
