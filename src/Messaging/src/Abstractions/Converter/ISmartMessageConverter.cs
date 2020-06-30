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
    /// An extended message converter supporting conversion hints
    /// </summary>
    public interface ISmartMessageConverter : IMessageConverter
    {
        /// <summary>
        /// Convert the payload of a message to a typed object.
        /// </summary>
        /// <param name="message">the input message</param>
        /// <param name="targetClass">the target type of the conversion</param>
        /// <param name="conversionHint">an extra object passed to the converter which may used for handling the conversion</param>
        /// <returns>the result of the conversion</returns>
        object FromMessage(IMessage message, Type targetClass, object conversionHint);

        /// <summary>
        /// Convert the payload of a message to a typed object.
        /// </summary>
        /// <typeparam name="T">the target type for the conversion</typeparam>
        /// <param name="message">the input message</param>
        /// <param name="conversionHint">an extra object passed to the converter which may used for handling the conversion</param>
        /// <returns>the result of the conversion</returns>
        T FromMessage<T>(IMessage message, object conversionHint);

        /// <summary>
        /// Create a message whose payload is the result of converting the given payload object
        /// to serialized form.
        /// </summary>
        /// <param name="payload">the object to convert</param>
        /// <param name="headers">optional headers for the message</param>
        /// <param name="conversionHint">an extra object passed to the converter which may used for handling the conversion</param>
        /// <returns>the new messagee or null if converter does not support the payload type</returns>
        IMessage ToMessage(object payload, IMessageHeaders headers, object conversionHint);
    }
}
