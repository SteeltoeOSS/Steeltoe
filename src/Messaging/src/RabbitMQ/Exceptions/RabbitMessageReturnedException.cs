// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
