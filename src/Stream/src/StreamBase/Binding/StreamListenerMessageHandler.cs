// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Handler;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Handler.Invocation;
using System;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binding;

public class StreamListenerMessageHandler : AbstractReplyProducingMessageHandler
{
    private readonly IInvocableHandlerMethod _invocableHandlerMethod;

    public StreamListenerMessageHandler(IApplicationContext context, IInvocableHandlerMethod invocableHandlerMethod, bool copyHeaders, IList<string> notPropagatedHeaders)
        : base(context)
    {
        _invocableHandlerMethod = invocableHandlerMethod;
        ShouldCopyRequestHeaders = copyHeaders;
        NotPropagatedHeaders = notPropagatedHeaders;
    }

    public bool IsVoid
    {
        get { return _invocableHandlerMethod.IsVoid; }
    }

    protected override bool ShouldCopyRequestHeaders { get; }

    public override void Initialize()
    {
        // Nothing to do
    }

    protected override object HandleRequestMessage(IMessage requestMessage)
    {
        try
        {
            // TODO:  Look at async task type methods
            var result = _invocableHandlerMethod.Invoke(requestMessage);
            return result;
        }
        catch (Exception e)
        {
            if (e is MessagingException)
            {
                throw;
            }
            else
            {
                throw new MessagingException(
                    requestMessage, $"Exception thrown while invoking {_invocableHandlerMethod.ShortLogMessage}", e);
            }
        }
    }
}
