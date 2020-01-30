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
    /// Strategy interface for resolving method parameters into argument values in the context of a given request.
    /// </summary>
    public interface IHandlerMethodArgumentResolver
    {
        /// <summary>
        /// Determine whether the given method parameter is supported by this resolver.
        /// </summary>
        /// <param name="parameter">the parameter info to consideer</param>
        /// <returns>true if it is supported</returns>
        bool SupportsParameter(ParameterInfo parameter);

        /// <summary>
        /// Resolves a method parameter into an argument value from a given message.
        /// </summary>
        /// <param name="parameter">the parameter info to consideer</param>
        /// <param name="message">the message</param>
        /// <returns>the resolved argument value</returns>
        object ResolveArgument(ParameterInfo parameter, IMessage message);
    }
}
