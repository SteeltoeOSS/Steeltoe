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

using Steeltoe.Common.Services;
using Steeltoe.Messaging.Handler.Invocation;
using System.Reflection;

namespace Steeltoe.Messaging.Handler.Attributes.Support
{
    /// <summary>
    /// A factory for invokable handler methods that is suitable to process an incoming message
    /// </summary>
    public interface IMessageHandlerMethodFactory : IServiceNameAware
    {
        /// <summary>
        /// Create the invokable handler method that can process the specified method endpoint.
        /// </summary>
        /// <param name="instance">the instance of the object</param>
        /// <param name="method">the method to invoke</param>
        /// <returns>a suitable invokable handler for the method</returns>
        IInvocableHandlerMethod CreateInvocableHandlerMethod(object instance, MethodInfo method);

        void Initialize();
    }
}
