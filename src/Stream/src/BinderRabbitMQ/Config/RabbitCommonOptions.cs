// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using RabbitConfig = Steeltoe.Messaging.RabbitMQ.Config;

namespace Steeltoe.Stream.Binder.Rabbit.Config
{
    public class RabbitCommonOptions
    {
        public const string DEAD_LETTER_EXCHANGE = "DLX";

        public string ExchangeType { get; set; }

        public bool? DeclareExchange { get; set; }

        public bool? ExchangeDurable { get; set; }

        public bool? ExchangeAutoDelete { get; set; }

        public bool? DelayedExchange { get; set; }

        public bool? QueueNameGroupOnly { get; set; }

        public bool? BindQueue { get; set; }

        public string BindingRoutingKey { get; set; }

        public string BindingRoutingKeyDelimiter { get; set; }

        public int? Ttl { get; set; }

        public int? Expires { get; set; }

        public int? MaxLength { get; set; }

        public int? MaxLengthBytes { get; set; }

        public int? MaxPriority { get; set; }

        public string DeadLetterQueueName { get; set; }

        public string DeadLetterExchange { get; set; }

        public string DeadLetterExchangeType { get; set; }

        public bool? DeclareDlx { get; set; } = true;

        public string DeadLetterRoutingKey { get; set; }

        public int? DlqTtl { get; set; }

        public int? DlqExpires { get; set; }

        public int? DlqMaxLength { get; set; }

        public int? DlqMaxLengthBytes { get; set; }

        public int? DlqMaxPriority { get; set; }

        public string DlqDeadLetterExchange { get; set; }

        public string DlqDeadLetterRoutingKey { get; set; }

        public bool? AutoBindDlq { get; set; }

        public string Prefix { get; set; }

        public bool? Lazy { get; set; }

        public bool? DlqLazy { get; set; }

        public string OverflowBehavior { get; set; }

        public string DlqOverflowBehavior { get; set; }

        public Dictionary<string, string> QueueBindingArguments { get; set; }

        public Dictionary<string, string> DlqBindingArguments { get; set; }

        public QuorumConfig Quorum { get; set; }

        public QuorumConfig DlqQuorum { get; set; }

        public bool? SingleActiveConsumer { get; set; }

        public bool? DlqSingleActiveConsumer { get; set; }

        internal void PostProcess(RabbitCommonOptions defaultOptions = null)
        {
            ExchangeType ??= defaultOptions != null ? defaultOptions.ExchangeType : RabbitConfig.ExchangeType.TOPIC;
            DeclareExchange ??= defaultOptions != null ? defaultOptions.DeclareExchange : true;
            ExchangeDurable ??= defaultOptions != null ? defaultOptions.ExchangeDurable : true;
            ExchangeAutoDelete ??= defaultOptions != null ? defaultOptions.ExchangeAutoDelete : false;
            DelayedExchange ??= defaultOptions != null ? defaultOptions.DelayedExchange : false;
            QueueNameGroupOnly ??= defaultOptions != null ? defaultOptions.QueueNameGroupOnly : false;
            BindQueue ??= defaultOptions != null ? defaultOptions.BindQueue : true;
            BindingRoutingKey ??= defaultOptions?.BindingRoutingKey;
            BindingRoutingKeyDelimiter ??= defaultOptions?.BindingRoutingKeyDelimiter;
            Ttl ??= defaultOptions?.Ttl;
            Expires ??= defaultOptions?.Expires;
            MaxLength ??= defaultOptions?.MaxLength;
            MaxLengthBytes ??= defaultOptions?.MaxLengthBytes;
            MaxPriority ??= defaultOptions?.MaxPriority;
            DeadLetterQueueName ??= defaultOptions?.DeadLetterQueueName;
            DeadLetterExchange ??= defaultOptions?.DeadLetterExchange;

            DeadLetterExchangeType ??=
                defaultOptions != null ? defaultOptions.DeadLetterExchangeType : RabbitConfig.ExchangeType.DIRECT;

            DeclareDlx ??= defaultOptions != null ? defaultOptions.DeclareDlx : true;
            DeadLetterRoutingKey ??= defaultOptions?.DeadLetterRoutingKey;
            DlqTtl ??= defaultOptions?.DlqTtl;
            DlqExpires ??= defaultOptions?.DlqExpires;
            DlqMaxLength ??= defaultOptions?.DlqMaxLength;
            DlqMaxLengthBytes ??= defaultOptions?.DlqMaxLengthBytes;
            DlqMaxPriority ??= defaultOptions?.DlqMaxPriority;
            DlqDeadLetterExchange ??= defaultOptions?.DlqDeadLetterExchange;
            DlqDeadLetterRoutingKey ??= defaultOptions?.DlqDeadLetterRoutingKey;
            AutoBindDlq ??= defaultOptions != null ? defaultOptions.AutoBindDlq : false;

            Prefix ??= defaultOptions != null ? defaultOptions.Prefix : string.Empty;
            Lazy ??= defaultOptions != null ? defaultOptions.Lazy : false;
            DlqLazy ??= defaultOptions != null ? defaultOptions.DlqLazy : false;
            OverflowBehavior ??= defaultOptions?.OverflowBehavior;
            DlqOverflowBehavior ??= defaultOptions?.DlqOverflowBehavior;
            SingleActiveConsumer ??= defaultOptions != null ? defaultOptions.SingleActiveConsumer : false;
            DlqSingleActiveConsumer ??= defaultOptions != null ? defaultOptions.DlqSingleActiveConsumer : false;
            Quorum ??= defaultOptions != null ? defaultOptions.Quorum : new QuorumConfig { Enabled = false };
            DlqQuorum ??= defaultOptions != null ? defaultOptions.DlqQuorum : new QuorumConfig { Enabled = false };

            QueueBindingArguments ??=
                defaultOptions != null ? defaultOptions.QueueBindingArguments : new Dictionary<string, string>();

            DlqBindingArguments ??= defaultOptions != null ? defaultOptions.DlqBindingArguments : new Dictionary<string, string>();
        }

        public class QuorumConfig
        {
            public bool? Enabled { get; set; }

            public int? InitialQuorumSize { get; set; }

            public int? DeliveryLimit { get; set; }
        }
    }
}
