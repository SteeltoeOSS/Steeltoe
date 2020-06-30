// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation
{
    public abstract class AbstractAsyncReturnValueHandler : IAsyncHandlerMethodReturnValueHandler
    {
        public void HandleReturnValue(object returnValue, ParameterInfo returnType, IMessage message)
        {
            throw new InvalidOperationException("Unexpected invocation");
        }

        public virtual bool IsAsyncReturnValue(object returnValue, ParameterInfo returnType)
        {
            return true;
        }

        public abstract bool SupportsReturnType(ParameterInfo returnType);
    }
}
