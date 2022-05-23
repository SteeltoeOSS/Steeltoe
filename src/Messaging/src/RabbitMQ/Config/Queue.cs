// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Steeltoe.Messaging.RabbitMQ.Config
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
            QueueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
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
