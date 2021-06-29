﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using System.Collections.Generic;

namespace Steeltoe.Messaging.RabbitMQ.Config
{
    public class QueueBuilder : AbstractBuilder
    {
        public class OverFlow
        {
            public static OverFlow DropHead { get; } = new OverFlow("drop-head");

            public static OverFlow RejectPublish { get; } = new OverFlow("reject-publish");

            public string Value { get; }

            private OverFlow(string value)
            {
                Value = value;
            }
        }

        public class MasterLocator
        {
            public static MasterLocator MinMasters { get; } = new MasterLocator("min-masters");

            public static MasterLocator ClientLocal { get; } = new MasterLocator("client-local");

            public static MasterLocator Random { get; } = new MasterLocator("random");

            public string Value { get; }

            private MasterLocator(string value)
            {
                Value = value;
            }
        }

        private static INamingStrategy _namingStrategy = Base64UrlNamingStrategy.DEFAULT;
        private readonly string _name;
        private bool _durable;
        private bool _exclusive;
        private bool _autoDelete;

        public static QueueBuilder Durable()
        {
            return Durable(_namingStrategy.GenerateName());
        }

        public static QueueBuilder Durable(string name)
        {
            var builder = new QueueBuilder(name);
            builder._durable = true;
            return builder;
        }

        public static QueueBuilder NonDurable()
        {
            return new QueueBuilder(_namingStrategy.GenerateName());
        }

        public static QueueBuilder NonDurable(string name)
        {
            return new QueueBuilder(name);
        }

        private QueueBuilder(string name)
        {
            _name = name;
        }

        public QueueBuilder Exclusive()
        {
            _exclusive = true;
            return this;
        }

        public QueueBuilder AutoDelete()
        {
            _autoDelete = true;
            return this;
        }

        public QueueBuilder WithArgument(string key, object value)
        {
            GetOrCreateArguments()[key] = value;
            return this;
        }

        public QueueBuilder WithArguments(Dictionary<string, object> arguments)
        {
            var args = GetOrCreateArguments();
            foreach (var arg in arguments)
            {
                args[arg.Key] = arg.Value;
            }

            return this;
        }

        public QueueBuilder TTL(int ttl)
        {
            return WithArgument("x-message-ttl", ttl);
        }

        public QueueBuilder Expires(int expires)
        {
            return WithArgument("x-expires", expires);
        }

        public QueueBuilder MaxLength(int count)
        {
            return WithArgument("x-max-length", count);
        }

        public QueueBuilder MaxLengthBytes(int bytes)
        {
            return WithArgument("x-max-length-bytes", bytes);
        }

        public QueueBuilder Overflow(OverFlow overflow)
        {
            return WithArgument("x-overflow", overflow.Value);
        }

        public QueueBuilder DeadLetterExchange(string dlx)
        {
            return WithArgument("x-dead-letter-exchange", dlx);
        }

        public QueueBuilder DeadLetterRoutingKey(string dlrk)
        {
            return WithArgument("x-dead-letter-routing-key", dlrk);
        }

        public QueueBuilder MaxPriority(int maxPriority)
        {
            return WithArgument("x-max-priority", maxPriority);
        }

        public QueueBuilder Lazy()
        {
            return WithArgument("x-queue-mode", "lazy");
        }

        public QueueBuilder Masterlocator(MasterLocator locator)
        {
            return WithArgument("x-queue-master-locator", locator.Value);
        }

        public QueueBuilder SingleActiveConsumer()
        {
            return WithArgument("x-single-active-consumer", true);
        }

        public QueueBuilder Quorum()
        {
            return WithArgument("x-queue-type", "quorum");
        }

        public QueueBuilder DeliveryLimit(int limit)
        {
            return WithArgument("x-delivery-limit", limit);
        }

        public IQueue Build()
        {
            return new Queue(_name, _durable, _exclusive, _autoDelete, Arguments);
        }
    }
}
