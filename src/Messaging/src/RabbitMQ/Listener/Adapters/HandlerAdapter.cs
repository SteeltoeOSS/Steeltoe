// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Steeltoe.Messaging.Handler.Invocation;

namespace Steeltoe.Messaging.RabbitMQ.Listener.Adapters;

public class HandlerAdapter
{
    public IInvocableHandlerMethod InvokerHandlerMethod { get; }

    public DelegatingInvocableHandler DelegatingHandler { get; }

    public object Instance
    {
        get
        {
            if (InvokerHandlerMethod != null)
            {
                return InvokerHandlerMethod.Handler;
            }

            return DelegatingHandler.Bean;
        }
    }

    public HandlerAdapter(IInvocableHandlerMethod invokerHandlerMethod)
    {
        InvokerHandlerMethod = invokerHandlerMethod;
        DelegatingHandler = null;
    }

    public HandlerAdapter(DelegatingInvocableHandler delegatingHandler)
    {
        InvokerHandlerMethod = null;
        DelegatingHandler = delegatingHandler;
    }

    public InvocationResult Invoke(IMessage message, params object[] providedArgs)
    {
        if (InvokerHandlerMethod != null)
        {
            return new InvocationResult(InvokerHandlerMethod.Invoke(message, providedArgs), null, InvokerHandlerMethod.Method.ReturnType,
                InvokerHandlerMethod.Handler, InvokerHandlerMethod.Method);
        }

        if (DelegatingHandler.HasDefaultHandler)
        {
            // Needed to avoid returning raw Message which matches Object
            object[] args = new object[providedArgs.Length + 1];
            args[0] = message.Payload;
            Array.Copy(providedArgs, 0, args, 1, providedArgs.Length);
            return DelegatingHandler.Invoke(message, args);
        }

        return DelegatingHandler.Invoke(message, providedArgs);
    }

    public string GetMethodAsString(object payload)
    {
        if (InvokerHandlerMethod != null)
        {
            return InvokerHandlerMethod.Method.ToString();
        }

        return DelegatingHandler.GetMethodNameFor(payload);
    }

    public MethodInfo GetMethodFor(object payload)
    {
        if (InvokerHandlerMethod != null)
        {
            return InvokerHandlerMethod.Method;
        }

        return DelegatingHandler.GetMethodFor(payload);
    }

    public Type GetReturnTypeFor(object payload)
    {
        if (InvokerHandlerMethod != null)
        {
            return InvokerHandlerMethod.Method.ReturnType;
        }

        return DelegatingHandler.GetMethodFor(payload).ReturnType;
    }

    public InvocationResult GetInvocationResultFor(object result, object inboundPayload)
    {
        if (InvokerHandlerMethod != null)
        {
            return new InvocationResult(result, null, InvokerHandlerMethod.Method.ReturnType, InvokerHandlerMethod.Handler, InvokerHandlerMethod.Method);
        }

        return DelegatingHandler.GetInvocationResultFor(result, inboundPayload);
    }
}
