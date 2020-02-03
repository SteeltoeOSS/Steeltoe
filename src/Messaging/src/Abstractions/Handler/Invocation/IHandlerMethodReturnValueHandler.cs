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
