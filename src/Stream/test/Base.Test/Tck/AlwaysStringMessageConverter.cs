// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using System;
using System.Text;

namespace Steeltoe.Stream.Tck
{
    public class AlwaysStringMessageConverter : AbstractMessageConverter
    {
        public AlwaysStringMessageConverter()
            : this(MimeType.ToMimeType("application/x-java-object"))
        {
        }

        public AlwaysStringMessageConverter(MimeType supportedMimeType)
            : base(supportedMimeType)
        {
        }

        protected override bool Supports(Type clazz)
        {
            return clazz == null || typeof(string).IsAssignableFrom(clazz);
        }

        protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
        {
            return this.GetType().Name;
        }

        protected override object ConvertToInternal(object payload, IMessageHeaders headers, object conversionHint)
        {
            return Encoding.UTF8.GetBytes((string)payload);
        }
    }
}
