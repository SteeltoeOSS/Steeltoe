// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using RabbitConfig = Steeltoe.Messaging.RabbitMQ.Config;

namespace Steeltoe.Stream.Binder.Rabbit.Config;

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
        if (ExchangeType == null)
        {
            ExchangeType = defaultOptions != null ? defaultOptions.ExchangeType : RabbitConfig.ExchangeType.TOPIC; // RabbitConfig.ExchangeType.DIRECT;
        }

        if (!DeclareExchange.HasValue)
        {
            DeclareExchange = defaultOptions != null ? defaultOptions.DeclareExchange : true;
        }

        if (!ExchangeDurable.HasValue)
        {
            ExchangeDurable = defaultOptions != null ? defaultOptions.ExchangeDurable : true;
        }

        if (!ExchangeAutoDelete.HasValue)
        {
            ExchangeAutoDelete = defaultOptions != null ? defaultOptions.ExchangeAutoDelete : false;
        }

        if (!DelayedExchange.HasValue)
        {
            DelayedExchange = defaultOptions != null ? defaultOptions.DelayedExchange : false;
        }

        if (!QueueNameGroupOnly.HasValue)
        {
            QueueNameGroupOnly = defaultOptions != null ? defaultOptions.QueueNameGroupOnly : false;
        }

        if (!BindQueue.HasValue)
        {
            BindQueue = defaultOptions != null ? defaultOptions.BindQueue : true;
        }

        if (BindingRoutingKey == null)
        {
            BindingRoutingKey = defaultOptions?.BindingRoutingKey;
        }

        if (BindingRoutingKeyDelimiter == null)
        {
            BindingRoutingKeyDelimiter = defaultOptions?.BindingRoutingKeyDelimiter;
        }

        if (!Ttl.HasValue)
        {
            Ttl = defaultOptions?.Ttl;
        }

        if (!Expires.HasValue)
        {
            Expires = defaultOptions?.Expires;
        }

        if (!MaxLength.HasValue)
        {
            MaxLength = defaultOptions?.MaxLength;
        }

        if (!MaxLengthBytes.HasValue)
        {
            MaxLengthBytes = defaultOptions?.MaxLengthBytes;
        }

        if (!MaxPriority.HasValue)
        {
            MaxPriority = defaultOptions?.MaxPriority;
        }

        if (DeadLetterQueueName == null)
        {
            DeadLetterQueueName = defaultOptions?.DeadLetterQueueName;
        }

        if (DeadLetterExchange == null)
        {
            DeadLetterExchange = defaultOptions?.DeadLetterExchange;
        }

        if (DeadLetterExchangeType == null)
        {
            DeadLetterExchangeType = defaultOptions != null ? defaultOptions.DeadLetterExchangeType : RabbitConfig.ExchangeType.DIRECT;
        }

        if (!DeclareDlx.HasValue)
        {
            DeclareDlx = defaultOptions != null ? defaultOptions.DeclareDlx : true;
        }

        if (DeadLetterRoutingKey == null)
        {
            DeadLetterRoutingKey = defaultOptions?.DeadLetterRoutingKey;
        }

        if (!DlqTtl.HasValue)
        {
            DlqTtl = defaultOptions?.DlqTtl;
        }

        if (!DlqExpires.HasValue)
        {
            DlqExpires = defaultOptions?.DlqExpires;
        }

        if (!DlqMaxLength.HasValue)
        {
            DlqMaxLength = defaultOptions?.DlqMaxLength;
        }

        if (!DlqMaxLengthBytes.HasValue)
        {
            DlqMaxLengthBytes = defaultOptions?.DlqMaxLengthBytes;
        }

        if (!DlqMaxPriority.HasValue)
        {
            DlqMaxPriority = defaultOptions?.DlqMaxPriority;
        }

        if (DlqDeadLetterExchange == null)
        {
            DlqDeadLetterExchange = defaultOptions?.DlqDeadLetterExchange;
        }

        if (DlqDeadLetterRoutingKey == null)
        {
            DlqDeadLetterRoutingKey = defaultOptions?.DlqDeadLetterRoutingKey;
        }

        if (!AutoBindDlq.HasValue)
        {
            AutoBindDlq = defaultOptions != null ? defaultOptions.AutoBindDlq : false;
        }

        if (Prefix == null)
        {
            Prefix = defaultOptions != null ? defaultOptions.Prefix : string.Empty;
        }

        if (!Lazy.HasValue)
        {
            Lazy = defaultOptions != null ? defaultOptions.Lazy : false;
        }

        if (!DlqLazy.HasValue)
        {
            DlqLazy = defaultOptions != null ? defaultOptions.DlqLazy : false;
        }

        if (OverflowBehavior == null)
        {
            OverflowBehavior = defaultOptions?.OverflowBehavior;
        }

        if (DlqOverflowBehavior == null)
        {
            DlqOverflowBehavior = defaultOptions?.DlqOverflowBehavior;
        }

        if (!SingleActiveConsumer.HasValue)
        {
            SingleActiveConsumer = defaultOptions != null ? defaultOptions.SingleActiveConsumer : false;
        }

        if (!DlqSingleActiveConsumer.HasValue)
        {
            DlqSingleActiveConsumer = defaultOptions != null ? defaultOptions.DlqSingleActiveConsumer : false;
        }

        if (Quorum == null)
        {
            Quorum = defaultOptions != null ? defaultOptions.Quorum : new QuorumConfig() { Enabled = false };
        }

        if (DlqQuorum == null)
        {
            DlqQuorum = defaultOptions != null ? defaultOptions.DlqQuorum : new QuorumConfig() { Enabled = false };
        }

        if (QueueBindingArguments == null)
        {
            QueueBindingArguments = defaultOptions != null ? defaultOptions.QueueBindingArguments : new Dictionary<string, string>();
        }

        if (DlqBindingArguments == null)
        {
            DlqBindingArguments = defaultOptions != null ? defaultOptions.DlqBindingArguments : new Dictionary<string, string>();
        }
    }

    public class QuorumConfig
    {
        public bool? Enabled { get; set; }

        public int? InitialQuorumSize { get; set; }

        public int? DeliveryLimit { get; set; }
    }
}