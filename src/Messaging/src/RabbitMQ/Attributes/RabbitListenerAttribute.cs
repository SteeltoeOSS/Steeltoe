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

namespace Steeltoe.Messaging.Rabbit.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
    public class RabbitListenerAttribute : Attribute
    {
        public RabbitListenerAttribute(params string[] queues)
        {
            Queues = queues;
        }

        public string Id { get; set; } = string.Empty;

        public string ContainerFactory { get; set; } = string.Empty;

        public string Queue
        {
            get
            {
                if (Queues.Length == 0)
                {
                    return null;
                }

                return Queues[0];
            }

            set
            {
                Queues = new string[] { value };
            }
        }

        public string[] Queues { get; set; }

        public string Binding
        {
            get
            {
                if (Bindings.Length == 0)
                {
                    return null;
                }

                return Bindings[0];
            }

            set
            {
                Bindings = new string[] { value };
            }
        }

        public string[] Bindings { get; set; } = new string[0];

        public bool Exclusive { get; set; } = false;

        public string Priority { get; set; } = string.Empty;

        public string Admin { get; set; } = string.Empty;

        public string ReturnExceptions { get; set; } = string.Empty;

        public string ErrorHandler { get; set; } = string.Empty;

        public string Concurrency { get; set; } = string.Empty;

        public string AutoStartup { get; set; } = string.Empty;

        public string AckMode { get; set; } = string.Empty;

        public string ReplyPostProcessor { get; set; } = string.Empty;

        public string Group { get; set; }
    }
}
