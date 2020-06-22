﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation
{
    /// <summary>
    /// TODO: Evaluate if this can be removed
    /// </summary>
    public interface IAsyncHandlerMethodReturnValueHandler : IHandlerMethodReturnValueHandler
    {
        /// <summary>
        /// TODO: Evaluate if this can be removed
        /// </summary>
        /// <param name="returnValue">the value</param>
        /// <param name="parameterInfo">the return type info</param>
        /// <returns>true if the return type represents a async value</returns>
        bool IsAsyncReturnValue(object returnValue, ParameterInfo parameterInfo);
    }
}
