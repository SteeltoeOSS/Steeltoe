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

using Steeltoe.Messaging.Rabbit.Data;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Listener.Exceptions
{
    public class ListenerExecutionFailedException : Exception
    {
        public ListenerExecutionFailedException(string message, Exception cause, params Message[] failedMessages)
            : base(message, cause)
        {
            FailedMessages.AddRange(failedMessages);
        }

        public Message FailedMessage
        {
            get
            {
                if (FailedMessages.Count > 0)
                {
                    return FailedMessages[0];
                }

                return null;
            }
        }

        public List<Message> FailedMessages { get; } = new List<Message>();
    }
}
