// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Rabbit.Exceptions
{
    public class RabbitMessageReturnedException : RabbitException
    {
        public RabbitMessageReturnedException(string message, IMessage returnedMessage, int replyCode, string replyText, string exchange, string routingKey)
            : base(message)
        {
            ReturnedMessage = returnedMessage;
            ReplyCode = replyCode;
            ReplyText = replyText;
            Exchange = exchange;
            RoutingKey = routingKey;
        }

        public IMessage ReturnedMessage { get; }

        public int ReplyCode { get; }

        public string ReplyText { get; }

        public string Exchange { get; }

        public string RoutingKey { get; }

        public override string ToString()
        {
            return "AmqpMessageReturnedException: "
                    + Message
                    + "[returnedMessage=" + ReturnedMessage + ", replyCode=" + ReplyCode
                    + ", replyText=" + ReplyText + ", exchange=" + Exchange + ", routingKey=" + RoutingKey
                    + "]";
        }
    }
}
