// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using RabbitConfig = Steeltoe.Messaging.RabbitMQ.Config;
using RabbitCore = Steeltoe.Messaging.RabbitMQ.Core;

namespace Steeltoe.Stream.Binder.Rabbit.Config
{
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

        public RabbitConfig.ContainerType? ContainerType { get; set; } = RabbitConfig.ContainerType.DIRECT;

        public string AnonymousGroupPrefix { get; set; }

        public bool? IsEnableBatching { get; set; }

        internal void PostProcess(RabbitConsumerOptions defaultOptions = null)
        {
            if (!Transacted.HasValue)
            {
                Transacted = defaultOptions?.Transacted ?? false;
            }

            if (!AcknowledgeMode.HasValue)
            {
                AcknowledgeMode = defaultOptions?.AcknowledgeMode ?? RabbitCore.AcknowledgeMode.AUTO;
            }

            if (!MaxConcurrency.HasValue)
            {
                MaxConcurrency = defaultOptions?.MaxConcurrency ?? 1;
            }

            if (!Prefetch.HasValue)
            {
                Prefetch = defaultOptions?.Prefetch ?? 1;
            }

            if (!BatchSize.HasValue)
            {
                BatchSize = defaultOptions?.BatchSize ?? 1;
            }

            if (!DurableSubscription.HasValue)
            {
                DurableSubscription = defaultOptions?.DurableSubscription ?? true;
            }

            if (!RepublishToDlq.HasValue)
            {
                RepublishToDlq = defaultOptions?.RepublishToDlq ?? true;
            }

            if (!RepublishDeliveryMode.HasValue)
            {
                RepublishDeliveryMode = defaultOptions?.RepublishDeliveryMode ?? RabbitCore.MessageDeliveryMode.PERSISTENT;
            }

            if (!RequeueRejected.HasValue)
            {
                RequeueRejected = defaultOptions?.RequeueRejected ?? false;
            }

            if (HeaderPatterns == null)
            {
                HeaderPatterns = defaultOptions?.HeaderPatterns ?? new List<string>() { "*" };
            }

            if (!RecoveryInterval.HasValue)
            {
                RecoveryInterval = defaultOptions?.RecoveryInterval ?? 5000;
            }

            if (!Exclusive.HasValue)
            {
                Exclusive = defaultOptions?.Exclusive ?? false;
            }

            if (!MissingQueuesFatal.HasValue)
            {
                MissingQueuesFatal = defaultOptions?.MissingQueuesFatal ?? false;
            }

            if (!QueueDeclarationRetries.HasValue)
            {
                QueueDeclarationRetries = defaultOptions?.QueueDeclarationRetries;
            }

            if (!FailedDeclarationRetryInterval.HasValue)
            {
                FailedDeclarationRetryInterval = defaultOptions?.FailedDeclarationRetryInterval;
            }

            if (ConsumerTagPrefix == null)
            {
                ConsumerTagPrefix = defaultOptions?.ConsumerTagPrefix;
            }

            if (!FrameMaxHeadroom.HasValue)
            {
                FrameMaxHeadroom = defaultOptions?.FrameMaxHeadroom ?? 20_000;
            }

            if (AnonymousGroupPrefix == null)
            {
                AnonymousGroupPrefix = defaultOptions?.AnonymousGroupPrefix ?? "anonymous.";
            }

            if (!IsEnableBatching.HasValue)
            {
                IsEnableBatching = defaultOptions?.IsEnableBatching ?? false;
            }

            base.PostProcess();
        }
    }
}
