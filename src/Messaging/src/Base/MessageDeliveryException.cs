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

using System;

namespace Steeltoe.Messaging
{
    public class MessageDeliveryException : MessagingException
    {
        public MessageDeliveryException(string description)
        : base(description)
        {
        }

        public MessageDeliveryException(IMessage undeliveredMessage)
        : base(undeliveredMessage)
        {
        }

        public MessageDeliveryException(IMessage undeliveredMessage, string description)
        : base(undeliveredMessage, description)
        {
        }

        public MessageDeliveryException(IMessage message, Exception cause)
        : base(message, cause)
        {
        }

        public MessageDeliveryException(IMessage undeliveredMessage, string description, Exception cause)
        : base(undeliveredMessage, description, cause)
        {
        }
    }
}
