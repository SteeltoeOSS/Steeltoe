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

namespace Steeltoe.Messaging.Converter
{
    /// <summary>
    /// Resolve the content type for a message
    /// </summary>
    public interface IContentTypeResolver
    {
        /// <summary>
        /// Determine the MimeType of a message from the given message headers
        /// </summary>
        /// <param name="headers">the headers to use</param>
        /// <returns>the resolved MimeType</returns>
        MimeType Resolve(IMessageHeaders headers);
    }
}
