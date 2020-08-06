// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Handler
{
    /// <summary>
    /// Contract for mapping conditions to messages.
    /// </summary>
    public interface IMessageCondition
    {
    }

    /// <summary>
    /// Contract for mapping conditions to messages
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
        /// <returns>results of the comparison</returns>
        int CompareTo(T other, IMessage message);
    }
}
