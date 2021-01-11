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

namespace Steeltoe.Integration.Endpoint
{
    public abstract class AbstractMessageSource<T> : AbstractExpressionEvaluator, IMessageSource<T>
    {
        public Dictionary<string, IExpression> HeaderExpressions { get; set; }

        public AbstractMessageSource(IApplicationContext context)
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

            IMessage message = null;
            var headers = EvaluateHeaders();
            if (result is AbstractMessageBuilder)
            {
                if (headers != null && headers.Count > 0)
                {
                    ((AbstractMessageBuilder)result).CopyHeaders(headers);
                }

                message = ((AbstractMessageBuilder)result).Build();
            }
            else if (result is IMessage)
            {
                message = (IMessage)result;
                if (headers != null && headers.Count > 0)
                {
                    // create a new Message from this one in order to apply headers
                    message = MessageBuilderFactory.FromMessage(message).CopyHeaders(headers).Build();
                }
            }
            else
            {
                message = MessageBuilderFactory.WithPayload(result).CopyHeaders(headers).Build();
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
}
