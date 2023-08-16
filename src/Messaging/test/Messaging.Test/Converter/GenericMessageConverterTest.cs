// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Steeltoe.Common.Converter;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Messaging.Test.Converter;

public sealed class GenericMessageConverterTest
{
    private readonly IConversionService _conversionService = new DefaultConversionService();

    [Fact]
    public void FromMessageWithConversion()
    {
        var converter = new GenericMessageConverter(_conversionService);
        IMessage content = MessageBuilder.WithPayload("33").Build();
        Assert.Equal(33, converter.FromMessage<int>(content));
    }

    [Fact]
    public void FromMessageNoConverter()
    {
        var converter = new GenericMessageConverter(_conversionService);
        IMessage content = MessageBuilder.WithPayload(1234L).Build();
        Assert.Null(converter.FromMessage<CultureInfo>(content));
    }

    [Fact]
    public void FromMessageWithFailedConversion()
    {
        var converter = new GenericMessageConverter(_conversionService);
        IMessage content = MessageBuilder.WithPayload("test not a number").Build();
        Assert.Throws<MessageConversionException>(() => converter.FromMessage<int>(content));
    }
}
