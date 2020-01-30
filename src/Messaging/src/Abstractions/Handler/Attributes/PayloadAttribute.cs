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
    ///  Attribute that binds a method parameter to the payload of a message. Can also
    ///  be used to associate a payload to a method invocation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = false)]
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
