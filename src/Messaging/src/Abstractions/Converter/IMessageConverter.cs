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

namespace Steeltoe.Messaging.Converter
{
    /// <summary>
    /// A converter to turn the payload of a message from serialized form to a typed
    /// object and vice versa.
    /// </summary>
    public interface IMessageConverter
    {
        /// <summary>
        /// Convert the payload of a message to a typed object.
        /// </summary>
        /// <param name="message">the input message</param>
        /// <param name="targetClass">the target type for the conversion</param>
        /// <returns>the result of the conversion</returns>
        object FromMessage(IMessage message, Type targetClass);

        /// <summary>
        /// Convert the payload of a message to a typed object.
        /// </summary>
        /// <typeparam name="T">the target type for the conversion</typeparam>
        /// <param name="message">the input message</param>
        /// <returns>the result of the conversion</returns>
        T FromMessage<T>(IMessage message);

        /// <summary>
        /// Create a message whose payload is the result of converting the given payload object
        /// to serialized form.
        /// </summary>
        /// <param name="payload">the object to convert</param>
        /// <param name="headers">optional headers for the message</param>
        /// <returns>the new messagee or null if converter does not support the payload type</returns>
        IMessage ToMessage(object payload, IMessageHeaders headers = null);
    }
}
