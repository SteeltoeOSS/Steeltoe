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

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Handler
{
    /// <summary>
    /// This defines the lowest-level strategy of processing a Message and returning
    /// some Object(or null). Implementations will be focused on generic concerns,
    /// such as invoking a method, running a script, or evaluating an expression.
    /// </summary>
    public interface IMessageProcessor
    {
        /// <summary>
        /// Process a message and return a value or null
        /// </summary>
        /// <param name="message">message to process</param>
        /// <returns>resulting object</returns>
        object ProcessMessage(IMessage message);
    }

    /// <summary>
    /// This defines the lowest-level strategy of processing a Message and returning
    /// some Object(or null). Implementations will be focused on generic concerns,
    /// such as invoking a method, running a script, or evaluating an expression.
    /// </summary>
    /// <typeparam name="T">the type of the processing result</typeparam>
    public interface IMessageProcessor<out T> : IMessageProcessor
    {
        /// <summary>
        /// Process a message and return a value or null
        /// </summary>
        /// <param name="message">message to process</param>
        /// <returns>result after processing</returns>
        new T ProcessMessage(IMessage message);
    }
}
