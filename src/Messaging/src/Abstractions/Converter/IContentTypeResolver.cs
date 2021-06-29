// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
