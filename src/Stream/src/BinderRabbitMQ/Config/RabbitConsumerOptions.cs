// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using RabbitConfig = Steeltoe.Messaging.RabbitMQ.Config;
using RabbitCore = Steeltoe.Messaging.RabbitMQ.Core;

namespace Steeltoe.Stream.Binder.Rabbit.Config;

public class RabbitConsumerOptions : RabbitCommonOptions
{
    public bool? Transacted { get; set; }

    public RabbitCore.AcknowledgeMode? AcknowledgeMode { get; set; }

    public int? MaxConcurrency { get; set; }

    public int? Prefetch { get; set; }

    public int? BatchSize { get; set; }

    public bool? DurableSubscription { get; set; }

    public bool? RepublishToDlq { get; set; }

    public RabbitCore.MessageDeliveryMode? RepublishDeliveryMode { get; set; }

    public bool? RequeueRejected { get; set; }

    public List<string> HeaderPatterns { get; set; }

    public int? RecoveryInterval { get; set; }

    public bool? Exclusive { get; set; }

    public bool? MissingQueuesFatal { get; set; }

    public int? QueueDeclarationRetries { get; set; }

    public long? FailedDeclarationRetryInterval { get; set; }

    public string ConsumerTagPrefix { get; set; }

    public int? FrameMaxHeadroom { get; set; }

    public RabbitConfig.ContainerType? ContainerType { get; set; } = RabbitConfig.ContainerType.Direct;

    public string AnonymousGroupPrefix { get; set; }

    public bool? IsEnableBatching { get; set; }

    internal void PostProcess(RabbitConsumerOptions defaultOptions = null)
    {
        Transacted ??= defaultOptions?.Transacted ?? false;
        AcknowledgeMode ??= defaultOptions?.AcknowledgeMode ?? RabbitCore.AcknowledgeMode.Auto;
        MaxConcurrency ??= defaultOptions?.MaxConcurrency ?? 1;
        Prefetch ??= defaultOptions?.Prefetch ?? 1;
        BatchSize ??= defaultOptions?.BatchSize ?? 1;
        DurableSubscription ??= defaultOptions?.DurableSubscription ?? true;
        RepublishToDlq ??= defaultOptions?.RepublishToDlq ?? true;
        RepublishDeliveryMode ??= defaultOptions?.RepublishDeliveryMode ?? RabbitCore.MessageDeliveryMode.Persistent;
        RequeueRejected ??= defaultOptions?.RequeueRejected ?? false;
        HeaderPatterns ??= defaultOptions?.HeaderPatterns ?? new List<string> { "*" };
        RecoveryInterval ??= defaultOptions?.RecoveryInterval ?? 5000;
        Exclusive ??= defaultOptions?.Exclusive ?? false;
        MissingQueuesFatal ??= defaultOptions?.MissingQueuesFatal ?? false;
        QueueDeclarationRetries ??= defaultOptions?.QueueDeclarationRetries;
        FailedDeclarationRetryInterval ??= defaultOptions?.FailedDeclarationRetryInterval;
        ConsumerTagPrefix ??= defaultOptions?.ConsumerTagPrefix;
        FrameMaxHeadroom ??= defaultOptions?.FrameMaxHeadroom ?? 20_000;
        AnonymousGroupPrefix ??= defaultOptions?.AnonymousGroupPrefix ?? "anonymous.";
        IsEnableBatching ??= defaultOptions?.IsEnableBatching ?? false;

        base.PostProcess();
    }
}
