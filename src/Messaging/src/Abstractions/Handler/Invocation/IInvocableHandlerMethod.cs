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
    /// Invokes the underlying method with argument values resolved from the current message.
    /// </summary>
    public interface IInvocableHandlerMethod
    {
        object Bean { get; }

        MethodInfo Method { get; }

        /// <summary>
        /// Gets a value indicating whether the return type of the method is void
        /// </summary>
        bool IsVoid { get; }

        /// <summary>
        /// Gets a value for message logging (TODO: Look to remove)
        /// </summary>
        string ShortLogMessage { get; }

        /// <summary>
        /// Invoke the underlying method after resolving its argument values in the context of the given message.
        /// </summary>
        /// <param name="requestMessage">the message being processed</param>
        /// <param name="args">given arguments matched by type, not resolved</param>
        /// <returns>the raw value returned from the invoked method</returns>
        object Invoke(IMessage requestMessage, params object[] args);
    }
}
