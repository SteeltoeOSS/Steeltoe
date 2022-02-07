// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        private readonly string[] _destinations;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendToAttribute"/> class.
        /// </summary>
        /// <param name="destinations">the destinations for the message created</param>
        public SendToAttribute(params string[] destinations)
        {
            _destinations = destinations;
        }

        /// <summary>
        /// Gets the destinations for any messages created by the method
        /// </summary>
        public virtual string[] Destinations
        {
            get { return _destinations; }
        }
    }
}
