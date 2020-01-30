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

namespace Steeltoe.Messaging.Handler
{
    /// <summary>
    /// Contract for mapping conditions to messages.
    /// TODO: Look at not exposing this (i.e. internal)
    /// </summary>
    public interface IMessageCondition
    {
    }

    /// <summary>
    /// Contract for mapping conditions to messages
    /// TODO: Look at not exposing this (i.e. internal)
    /// </summary>
    /// <typeparam name="T">the kind of condition that this condition can be combined with</typeparam>
    public interface IMessageCondition<T> : IMessageCondition
    {
        /// <summary>
        /// Define the rules for combining this condition with another.
        /// </summary>
        /// <param name="other">the condition to combine with</param>
        /// <returns>the resulting message condition</returns>
        T Combine(T other);

        /// <summary>
        /// Check if this condition matches the given Message and returns a
        /// potentially new condition with content tailored to the current message.
        /// </summary>
        /// <param name="message">the message under process</param>
        /// <returns>a condition instance in case of a match; or null if no match</returns>
        T GetMatchingCondition(IMessage message);

        /// <summary>
        /// Compare this condition to another in the context of a specific message.
        /// </summary>
        /// <param name="other">the other condition to compare to</param>
        /// <param name="message">the message under process</param>
        /// <returns>results of the comparision</returns>
        int CompareTo(T other, IMessage message);
    }
}
