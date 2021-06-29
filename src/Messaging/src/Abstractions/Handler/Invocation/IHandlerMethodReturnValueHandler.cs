// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation
{
    /// <summary>
    /// Strategy interface to handle the value returned from the invocation of a method handling a Message.
    /// </summary>
    public interface IHandlerMethodReturnValueHandler
    {
        /// <summary>
        /// Determine whether the given method return type is supported by this handler.
        /// </summary>
        /// <param name="returnType">the return parameter info</param>
        /// <returns>true if it supports the supplied return type</returns>
        bool SupportsReturnType(ParameterInfo returnType);

        /// <summary>
        /// Handle the given return value.
        /// </summary>
        /// <param name="returnValue">the value returned from the handler method</param>
        /// <param name="returnType">the type of the return value</param>
        /// <param name="message">the message that was passed to the handler</param>
        void HandleReturnValue(object returnValue, ParameterInfo returnType, IMessage message);
    }
}
