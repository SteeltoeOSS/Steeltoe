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

using Steeltoe.Common.Services;
using Steeltoe.Messaging.Rabbit.Core;
using System;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Config
{
    // TODO: This is a AMQP class
    public class Queue : AbstractDeclarable, ICloneable, IServiceNameAware
    {
        public const string X_QUEUE_MASTER_LOCATOR = "x-queue-master-locator";

        public Queue(string name)
        : this(name, true, false, false)
        {
        }

        public Queue(string name, bool durable)
        : this(name, durable, false, false, null)
        {
        }

        public Queue(string name, bool durable, bool exclusive, bool autoDelete)
        : this(name, durable, exclusive, autoDelete, null)
        {
        }

        public Queue(string name, bool durable, bool exclusive, bool autoDelete, Dictionary<string, object> arguments)
            : base(arguments)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            ActualName = !string.IsNullOrEmpty(name) ? name : (Base64UrlNamingStrategy.DEFAULT.GenerateName() + "_awaiting_declaration");
            Durable = durable;
            Exclusive = exclusive;
            AutoDelete = autoDelete;
        }

        public string Name { get; set; }

        public string ActualName { get; set; }

        public bool Durable { get; set; }

        public bool Exclusive { get; set; }

        public bool AutoDelete { get; set; }

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
            var queue = new Queue(Name, Durable, Exclusive, AutoDelete, new Dictionary<string, object>(Arguments));
            queue.ActualName = ActualName;
            return queue;
        }

        public override string ToString()
        {
            return "Queue [name=" + Name + ", durable=" + Durable + ", autoDelete=" + AutoDelete
                    + ", exclusive=" + Exclusive + ", arguments=" + Arguments
                    + ", actualName=" + ActualName + "]";
        }
    }
}
