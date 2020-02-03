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

using Steeltoe.Common.Util;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Converter
{
    /// <summary>
    /// An implementation provides a factory for obtaining message converters
    /// </summary>
    public interface IMessageConverterFactory
    {
        /// <summary>
        /// Obtain a message converter for the given MimeType
        /// </summary>
        /// <param name="mimeType">the MimeType to obtain a converter for</param>
        /// <returns>a message converter or null if no converter exists</returns>
        IMessageConverter GetMessageConverterForType(MimeType mimeType);

        /// <summary>
        /// Gets a single composite message converter for all registered converters
        /// </summary>
        IMessageConverter MessageConverterForAllRegistered { get; }

        /// <summary>
        /// Gets all the message converters provided by this factory
        /// </summary>
        IList<IMessageConverter> AllRegistered { get; }
    }
}
