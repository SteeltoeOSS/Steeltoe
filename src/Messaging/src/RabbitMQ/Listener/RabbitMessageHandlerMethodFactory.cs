// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Converter;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.RabbitMQ.Support.Converter;
using System.Text;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class RabbitMessageHandlerMethodFactory : DefaultMessageHandlerMethodFactory
{
    public new const string DefaultServiceName = nameof(RabbitMessageHandlerMethodFactory);

    public RabbitMessageHandlerMethodFactory()
        : base(new DefaultConversionService())
    {
        var defService = ConversionService as DefaultConversionService;
        defService.AddConverter(new BytesToStringConverter(Charset));
    }

    public override string ServiceName { get; set; } = DefaultServiceName;

    public Encoding Charset { get; set; } = EncodingUtils.Utf8;
}
