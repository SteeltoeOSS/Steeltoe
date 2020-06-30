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

using Steeltoe.Messaging.Rabbit.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Steeltoe.Messaging.Rabbit.Config
{
    public class Queue : AbstractDeclarable, IQueue, ICloneable
    {
        public const string X_QUEUE_MASTER_LOCATOR = "x-queue-master-locator";

        public Queue(string queueName)
        : this(queueName, true, false, false)
        {
        }

        public Queue(string queueName, bool durable)
        : this(queueName, durable, false, false, null)
        {
        }

        public Queue(string queueName, bool durable, bool exclusive, bool autoDelete)
        : this(queueName, durable, exclusive, autoDelete, null)
        {
        }

        public Queue(string queueName, bool durable, bool exclusive, bool autoDelete, Dictionary<string, object> arguments)
            : base(arguments)
        {
            if (queueName == null)
            {
                throw new ArgumentNullException(nameof(queueName));
            }

            QueueName = queueName;
            ServiceName = !string.IsNullOrEmpty(queueName) ? queueName : "queue@" + RuntimeHelpers.GetHashCode(this);
            ActualName = !string.IsNullOrEmpty(queueName) ? queueName : (Base64UrlNamingStrategy.DEFAULT.GenerateName() + "_awaiting_declaration");
            IsDurable = durable;
            IsExclusive = exclusive;
            IsAutoDelete = autoDelete;
        }

        public string ServiceName { get; set; }

        public string QueueName { get; set; }

        public string ActualName { get; set; }

        public bool IsDurable { get; set; }

        public bool IsExclusive { get; set; }

        public bool IsAutoDelete { get; set; }

        public string MasterLocator
        {
            get
            {
                Arguments.TryGetValue(X_QUEUE_MASTER_LOCATOR, out var result);
                return result as string;
            }

            set
            {
                if (value == null)
                {
                    RemoveArgument(X_QUEUE_MASTER_LOCATOR);
                }
                else
                {
                    AddArgument(X_QUEUE_MASTER_LOCATOR, value);
                }
            }
        }

        public object Clone()
        {
            var queue = new Queue(QueueName, IsDurable, IsExclusive, IsAutoDelete, new Dictionary<string, object>(Arguments));
            queue.ActualName = ActualName;
            return queue;
        }

        public override string ToString()
        {
            return "Queue [name=" + QueueName + ", durable=" + IsDurable + ", autoDelete=" + IsAutoDelete
                    + ", exclusive=" + IsExclusive + ", arguments=" + Arguments
                    + ", actualName=" + ActualName + "]";
        }
    }
}
