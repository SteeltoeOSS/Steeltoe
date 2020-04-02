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

using Steeltoe.Common.Expression;
using System;
using System.Reflection;

namespace Steeltoe.Messaging.Rabbit.Listener.Adapters
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
            return "InvocationResult [returnValue=" + ReturnValue
                    + (SendTo != null ? ", sendTo=" + SendTo : string.Empty)
                    + ", returnType=" + ReturnType
                    + ", instance=" + Instance
                    + ", method=" + Method
                    + "]";
        }
    }
}
