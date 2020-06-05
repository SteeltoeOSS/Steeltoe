// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Handler.Invocation;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support
{
    /// <summary>
    /// A factory for invokable handler methods that is suitable to process an incoming message
    /// </summary>
    public interface IMessageHandlerMethodFactory
    {
        /// <summary>
        /// Create the invokable handler method that can process the specified method endpoint.
        /// </summary>
        /// <param name="instance">the instance of the object</param>
        /// <param name="method">the method to invoke</param>
        /// <returns>a suitable invokable handler for the method</returns>
        IInvocableHandlerMethod CreateInvocableHandlerMethod(object instance, MethodInfo method);
    }
}
