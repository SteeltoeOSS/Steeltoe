// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation
{
    public class HandlerMethodReturnValueHandlerComposite : IAsyncHandlerMethodReturnValueHandler
    {
        private readonly List<IHandlerMethodReturnValueHandler> _returnValueHandlers = new ();

        public IList<IHandlerMethodReturnValueHandler> ReturnValueHandlers
        {
            get { return new List<IHandlerMethodReturnValueHandler>(_returnValueHandlers); }
        }

        public void Clear()
        {
            _returnValueHandlers.Clear();
        }

        public HandlerMethodReturnValueHandlerComposite AddHandler(IHandlerMethodReturnValueHandler returnValueHandler)
        {
            _returnValueHandlers.Add(returnValueHandler);
            return this;
        }

        public HandlerMethodReturnValueHandlerComposite AddHandlers(IList<IHandlerMethodReturnValueHandler> handlers)
        {
            if (handlers != null)
            {
                _returnValueHandlers.AddRange(handlers);
            }

            return this;
        }

        public bool SupportsReturnType(ParameterInfo returnType)
        {
            return GetReturnValueHandler(returnType) != null;
        }

        public void HandleReturnValue(object returnValue, ParameterInfo returnType, IMessage message)
        {
            var handler = GetReturnValueHandler(returnType);
            if (handler == null)
            {
                throw new InvalidOperationException("No handler for return value type: " + returnType.ParameterType);
            }

            // if (logger.isTraceEnabled())
            // {
            //    logger.trace("Processing return value with " + handler);
            // }
            handler.HandleReturnValue(returnValue, returnType, message);
        }

        public bool IsAsyncReturnValue(object returnValue, ParameterInfo returnType)
            => GetReturnValueHandler(returnType) is IAsyncHandlerMethodReturnValueHandler handler1 &&
                    handler1.IsAsyncReturnValue(returnValue, returnType);

        private IHandlerMethodReturnValueHandler GetReturnValueHandler(ParameterInfo returnType)
        {
            for (var i = 0; i < _returnValueHandlers.Count; i++)
            {
                var handler = _returnValueHandlers[i];
                if (handler.SupportsReturnType(returnType))
                {
                    return handler;
                }
            }

            return null;
        }
    }
}
