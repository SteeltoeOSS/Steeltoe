// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.Handler.Attributes
{
    /// <summary>
    ///  Attribute that binds a method parameter to the payload of a message. Can also
    ///  be used to associate a payload to a method invocation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
    public class PayloadAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PayloadAttribute"/> class.
        /// </summary>
        public PayloadAttribute()
        {
            Expression = string.Empty;
            Required = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PayloadAttribute"/> class.
        /// </summary>
        /// <param name="expression">expression to be evaluated against the payload</param>
        /// <param name="required">whether payload content is required</param>
        public PayloadAttribute(string expression, bool required = true)
        {
            Expression = expression;
            Required = required;
        }

        /// <summary>
        /// Gets or sets the expression to be evaluated against the payload
        /// </summary>
        public virtual string Expression { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the payload content is required
        /// </summary>
        public virtual bool Required { get; set; }
    }
}
