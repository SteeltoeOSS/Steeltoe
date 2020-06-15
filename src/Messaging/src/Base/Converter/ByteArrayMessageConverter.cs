﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.Messaging.Converter
{
    public class ByteArrayMessageConverter : AbstractMessageConverter
    {
        public ByteArrayMessageConverter()
        : base(MimeTypeUtils.APPLICATION_OCTET_STREAM)
        {
        }

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
