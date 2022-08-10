// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;

namespace Steeltoe.Stream.Tck;

public class AlwaysStringMessageConverter : AbstractMessageConverter
{
    public const string DefaultServiceName = nameof(AlwaysStringMessageConverter);

    public override string ServiceName { get; set; } = DefaultServiceName;

    public AlwaysStringMessageConverter()
        : this(MimeType.ToMimeType("application/x-java-object"))
    {
    }

    public AlwaysStringMessageConverter(MimeType supportedMimeType)
        : base(supportedMimeType)
    {
    }

    protected override bool Supports(Type type)
    {
        return type == null || typeof(string).IsAssignableFrom(type);
    }

    protected override object ConvertFromInternal(IMessage message, Type targetClass, object conversionHint)
    {
        return GetType().Name;
    }

    protected override object ConvertToInternal(object payload, IMessageHeaders headers, object conversionHint)
    {
        return Encoding.UTF8.GetBytes((string)payload);
    }
}
