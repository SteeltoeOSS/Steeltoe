// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.Messaging.Converter;

public class DefaultContentTypeResolver : IContentTypeResolver
{
    public MimeType DefaultMimeType { get; set; }

    public MimeType Resolve(IMessageHeaders headers)
    {
        if (headers == null || !headers.ContainsKey(MessageHeaders.CONTENT_TYPE))
        {
            return DefaultMimeType;
        }

        var value = headers[MessageHeaders.CONTENT_TYPE];
        if (value == null)
        {
            return null;
        }
        else if (value is MimeType mimeType)
        {
            return mimeType;
        }
        else if (value is string stringVal)
        {
            return MimeType.ToMimeType(stringVal);
        }
        else
        {
            throw new ArgumentException("Unknown type for contentType header value: " + value.GetType());
        }
    }

    public override string ToString() => "DefaultContentTypeResolver[" + "defaultMimeType=" + DefaultMimeType + "]";
}