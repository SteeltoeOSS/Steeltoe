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
    public class ByteArrayMessageConverter : AbstractMessageConverter
    {
        public const string DEFAULT_SERVICE_NAME = nameof(ByteArrayMessageConverter);

        public ByteArrayMessageConverter()
        : base(MimeTypeUtils.APPLICATION_OCTET_STREAM)
        {
        }

        public override string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

        protected override bool Supports(Type clazz)
        {
            return typeof(byte[]) == clazz;
        }

        protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
        {
            return message.Payload;
        }

        protected override object ConvertToInternal(object payload, IMessageHeaders headers, object conversionHint)
        {
            return payload;
        }
    }
}
