﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
