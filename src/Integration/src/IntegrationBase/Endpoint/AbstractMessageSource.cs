// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Integration.Expression;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using System.Collections.Generic;

namespace Steeltoe.Integration.Endpoint;

public abstract class AbstractMessageSource<T> : AbstractExpressionEvaluator, IMessageSource<T>
{
    public Dictionary<string, IExpression> HeaderExpressions { get; set; }

    protected AbstractMessageSource(IApplicationContext context)
        : base(context)
    {
    }

    public IMessage<T> Receive()
    {
        return BuildMessage(DoReceive());
    }

    IMessage IMessageSource.Receive()
    {
        return BuildMessage(DoReceive());
    }

    protected virtual IMessage<T> BuildMessage(object result)
    {
        if (result == null)
        {
            return null;
        }

        var headers = EvaluateHeaders();

        IMessage message;
        switch (result)
        {
            case AbstractMessageBuilder amBuilder:
                if (headers != null && headers.Count > 0)
                {
                    amBuilder.CopyHeaders(headers);
                }

                message = amBuilder.Build();
                break;
            case IMessage mResult:
                message = mResult;
                if (headers != null && headers.Count > 0)
                {
                    // create a new Message from this one in order to apply headers
                    message = MessageBuilderFactory.FromMessage(message).CopyHeaders(headers).Build();
                }

                break;
            default:
                message = MessageBuilderFactory.WithPayload(result).CopyHeaders(headers).Build();
                break;
        }

        return (IMessage<T>)message;
    }

    protected abstract object DoReceive();

    private IDictionary<string, object> EvaluateHeaders()
    {
        if (HeaderExpressions == null || HeaderExpressions.Count == 0)
        {
            return null;
        }

        return ExpressionEvalDictionary.From(HeaderExpressions).UsingEvaluationContext(EvaluationContext).Build();
    }
}
