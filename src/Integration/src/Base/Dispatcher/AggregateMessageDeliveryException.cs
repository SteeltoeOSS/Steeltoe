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

using Steeltoe.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Integration.Dispatcher
{
    public class AggregateMessageDeliveryException : MessageDeliveryException
    {
        private readonly List<Exception> _aggregatedExceptions;

        public AggregateMessageDeliveryException(IMessage undeliveredMessage, string description, List<Exception> aggregatedExceptions)
        : base(undeliveredMessage, description, aggregatedExceptions[0])
        {
            _aggregatedExceptions = new List<Exception>(aggregatedExceptions);
        }

        public List<Exception> AggregatedExceptions
        {
            get { return new List<Exception>(_aggregatedExceptions); }
        }

        public override string Message
        {
            get
            {
                var baseMessage = base.Message;
                var message = new StringBuilder(AppendPeriodIfNecessary(baseMessage) + " Multiple causes:\n");
                foreach (var exception in _aggregatedExceptions)
                {
                    message.Append("    " + exception.Message + "\n");
                }

                message.Append("See below for the stacktrace of the first cause.");
                return message.ToString();
            }
        }

        private string AppendPeriodIfNecessary(string baseMessage)
        {
            if (string.IsNullOrEmpty(baseMessage))
            {
                return string.Empty;
            }
            else if (!baseMessage.EndsWith("."))
            {
                return baseMessage + ".";
            }
            else
            {
                return baseMessage;
            }
        }
    }
}
