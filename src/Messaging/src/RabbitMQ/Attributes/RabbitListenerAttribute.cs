// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.RabbitMQ.Attributes
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
