// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.RabbitMQ.Support.Converter;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class RabbitMessageHandlerMethodFactory : DefaultMessageHandlerMethodFactory
{
    public new const string DEFAULT_SERVICE_NAME = nameof(RabbitMessageHandlerMethodFactory);

    public RabbitMessageHandlerMethodFactory()
        : base(new DefaultConversionService())
    {
        var defService = ConversionService as DefaultConversionService;
        defService.AddConverter(new BytesToStringConverter(Charset));
    }

    public override string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

    public Encoding Charset { get; set; } = EncodingUtils.Utf8;
}