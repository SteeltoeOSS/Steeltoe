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
