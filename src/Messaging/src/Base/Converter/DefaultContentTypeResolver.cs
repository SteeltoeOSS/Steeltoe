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
using System;

namespace Steeltoe.Messaging.Converter
{
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
            else if (value is MimeType)
            {
                return (MimeType)value;
            }
            else if (value is string)
            {
                return MimeType.ToMimeType((string)value);
            }
            else
            {
                throw new ArgumentException("Unknown type for contentType header value: " + value.GetType());
            }
        }

        public override string ToString()
        {
            return "DefaultContentTypeResolver[" + "defaultMimeType=" + DefaultMimeType + "]";
        }
    }
}
