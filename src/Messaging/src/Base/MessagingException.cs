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
    public class MessagingException : Exception
    {
        private readonly IMessage failedMessage;

        public MessagingException(IMessage message)
        : base(null, null)
        {
            failedMessage = message;
        }

        public MessagingException(string description)
        : base(description)
        {
            failedMessage = null;
        }

        public MessagingException(string description, Exception cause)
        : base(description, cause)
        {
            failedMessage = null;
        }

        public MessagingException(IMessage message, string description)
        : base(description)
        {
            failedMessage = message;
        }

        public MessagingException(IMessage message, Exception cause)
        : base(null, cause)
        {
            failedMessage = message;
        }

        public MessagingException(IMessage message, string description, Exception cause)
        : base(description, cause)
        {
            failedMessage = message;
        }

        public IMessage FailedMessage
        {
            get { return failedMessage; }
        }

        public override string ToString()
        {
            return base.ToString() + (failedMessage == null ? string.Empty
                    : (", failedMessage=" + failedMessage));
        }
    }
}
