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
using System.Collections.Generic;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Invocation
{
    public class HandlerMethodReturnValueHandlerComposite : IAsyncHandlerMethodReturnValueHandler
    {
        private readonly List<IHandlerMethodReturnValueHandler> returnValueHandlers = new List<IHandlerMethodReturnValueHandler>();

        public IList<IHandlerMethodReturnValueHandler> ReturnValueHandlers
        {
            get { return new List<IHandlerMethodReturnValueHandler>(returnValueHandlers); }
        }

        public void Clear()
        {
            returnValueHandlers.Clear();
        }

        public HandlerMethodReturnValueHandlerComposite AddHandler(IHandlerMethodReturnValueHandler returnValueHandler)
        {
            returnValueHandlers.Add(returnValueHandler);
            return this;
        }

        public HandlerMethodReturnValueHandlerComposite AddHandlers(IList<IHandlerMethodReturnValueHandler> handlers)
        {
            if (handlers != null)
            {
                returnValueHandlers.AddRange(handlers);
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
        {
            var handler = GetReturnValueHandler(returnType);
            return handler is IAsyncHandlerMethodReturnValueHandler &&
                    ((IAsyncHandlerMethodReturnValueHandler)handler).IsAsyncReturnValue(returnValue, returnType);
        }

        // public ListenableFuture<?> toListenableFuture(Object returnValue, MethodParameter returnType)
        // {
        //    HandlerMethodReturnValueHandler handler = getReturnValueHandler(returnType);
        //    Assert.state(handler instanceof AsyncHandlerMethodReturnValueHandler,
        //            "AsyncHandlerMethodReturnValueHandler required");
        //    return ((AsyncHandlerMethodReturnValueHandler)handler).toListenableFuture(returnValue, returnType);
        // }
        private IHandlerMethodReturnValueHandler GetReturnValueHandler(ParameterInfo returnType)
        {
            for (var i = 0; i < returnValueHandlers.Count; i++)
            {
                var handler = returnValueHandlers[i];
                if (handler.SupportsReturnType(returnType))
                {
                    return handler;
                }
            }

            return null;
        }
    }
}
