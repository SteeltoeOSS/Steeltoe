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
    ///  Attribute that indicates a method's return value should be converted to a
    ///  message if necessary and sent to the specified destination.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class SendToAttribute : Attribute
    {
        private readonly string[] destinations;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendToAttribute"/> class.
        /// </summary>
        /// <param name="destinations">the destinations for the message created</param>
        public SendToAttribute(params string[] destinations)
        {
            this.destinations = destinations;
        }

        /// <summary>
        /// Gets the destinations for any messages created by the method
        /// </summary>
        public virtual string[] Destinations
        {
            get { return destinations; }
        }
    }
}
