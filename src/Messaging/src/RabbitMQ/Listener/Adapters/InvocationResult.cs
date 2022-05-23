// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Expression.Internal;
using System;
using System.Reflection;

namespace Steeltoe.Messaging.RabbitMQ.Listener.Adapters
{
    public class InvocationResult
    {
        public InvocationResult(object result, IExpression sendTo, Type returnType, object instance, MethodInfo method)
        {
            ReturnValue = result;
            SendTo = sendTo;
            ReturnType = returnType;
            Instance = instance;
            Method = method;
        }

        public object ReturnValue { get; }

        public IExpression SendTo { get; }

        public Type ReturnType { get; }

        public object Instance { get; }

        public MethodInfo Method { get; }

        public override string ToString()
        {
            return
                $"InvocationResult [returnValue={ReturnValue}{(SendTo != null ? $", sendTo={SendTo}" : string.Empty)}, returnType={ReturnType}, instance={Instance}, method={Method}]";
        }
    }
}
