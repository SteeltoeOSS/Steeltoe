// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Converter;
using Steeltoe.Stream.Binder;
using System;

namespace Steeltoe.Stream.Binding;

public class MessageSourceBindingTargetFactory : AbstractBindingTargetFactory<IPollableMessageSource>
{
    private readonly IMessageChannelAndSourceConfigurer _messageConfigurer;
    private readonly ISmartMessageConverter _messageConverter;

    public MessageSourceBindingTargetFactory(IApplicationContext context, ISmartMessageConverter messageConverter, CompositeMessageChannelConfigurer messageConfigurer)
        : base(context)
    {
        _messageConfigurer = messageConfigurer;
        _messageConverter = messageConverter;
    }

    public override IPollableMessageSource CreateInput(string name)
    {
        var chan = new DefaultPollableMessageSource(ApplicationContext, _messageConverter);
        _messageConfigurer.ConfigurePolledMessageSource(chan, name);
        return chan;
    }

    public override IPollableMessageSource CreateOutput(string name)
    {
        throw new InvalidOperationException();
    }
}
