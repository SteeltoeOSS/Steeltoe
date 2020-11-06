// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Integration.Rabbit.Support
{
    public class ReturnedRabbitMessageException : MessagingException
    {
        public ReturnedRabbitMessageException(IMessage message, int replyCode, string replyText, string exchange, string routingKey)
            : base(message)
        {
            ReplyCode = replyCode;
            ReplyText = replyText;
            Exchange = exchange;
            RoutingKey = routingKey;
        }

        public int ReplyCode { get; }

        public string ReplyText { get; }

        public string Exchange { get; }

        public string RoutingKey { get; }

        public override string ToString()
        {
            return base.ToString() + ", replyCode=" + ReplyCode
                    + ", replyText=" + ReplyText + ", exchange=" + Exchange + ", routingKey=" + RoutingKey
                    + "]";
        }
    }
}
