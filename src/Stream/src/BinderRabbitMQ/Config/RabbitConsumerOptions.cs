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
        public RabbitConsumerOptions()
            : base()
        {
        //    PostProcess();
        }

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

        internal void PostProcess(RabbitConsumerOptions defaultOptions = null)
        {
            if (!Transacted.HasValue)
            {
                Transacted = defaultOptions != null ? defaultOptions.Transacted : false;
            }

            if (!AcknowledgeMode.HasValue)
            {
                AcknowledgeMode = defaultOptions != null ? defaultOptions.AcknowledgeMode : RabbitCore.AcknowledgeMode.AUTO;
            }

            if (!MaxConcurrency.HasValue)
            {
                MaxConcurrency = defaultOptions != null ? defaultOptions.MaxConcurrency : 1;
            }

            if (!Prefetch.HasValue)
            {
                Prefetch = defaultOptions != null ? defaultOptions.Prefetch : 1;
            }

            if (!BatchSize.HasValue)
            {
                BatchSize = defaultOptions != null ? defaultOptions.BatchSize : 1;
            }

            if (!DurableSubscription.HasValue)
            {
                DurableSubscription = defaultOptions != null ? defaultOptions.DurableSubscription : true;
            }

            if (!RepublishToDlq.HasValue)
            {
                RepublishToDlq = defaultOptions != null ? defaultOptions.RepublishToDlq : true;
            }

            if (!RepublishDeliveryMode.HasValue)
            {
                RepublishDeliveryMode = defaultOptions != null ? defaultOptions.RepublishDeliveryMode : RabbitCore.MessageDeliveryMode.PERSISTENT;
            }

            if (!RequeueRejected.HasValue)
            {
                RequeueRejected = defaultOptions != null ? defaultOptions.RequeueRejected : false;
            }

            if (HeaderPatterns == null)
            {
                HeaderPatterns = defaultOptions != null ? defaultOptions.HeaderPatterns : new List<string>() { "*" };
            }

            if (!RecoveryInterval.HasValue)
            {
                RecoveryInterval = defaultOptions != null ? defaultOptions.RecoveryInterval : 5000;
            }

            if (!Exclusive.HasValue)
            {
                Exclusive = defaultOptions != null ? defaultOptions.Exclusive : false;
            }

            if (!MissingQueuesFatal.HasValue)
            {
                MissingQueuesFatal = defaultOptions != null ? defaultOptions.MissingQueuesFatal : false;
            }

            if (!QueueDeclarationRetries.HasValue)
            {
                QueueDeclarationRetries = defaultOptions != null ? defaultOptions.QueueDeclarationRetries : null;
            }

            if (!FailedDeclarationRetryInterval.HasValue)
            {
                FailedDeclarationRetryInterval = defaultOptions != null ? defaultOptions.FailedDeclarationRetryInterval : null;
            }

            if (ConsumerTagPrefix == null)
            {
                ConsumerTagPrefix = defaultOptions != null ? defaultOptions.ConsumerTagPrefix : null;
            }

            if (!FrameMaxHeadroom.HasValue)
            {
                FrameMaxHeadroom = defaultOptions != null ? defaultOptions.FrameMaxHeadroom : 20_000;
            }

            if (AnonymousGroupPrefix == null)
            {
                AnonymousGroupPrefix = defaultOptions != null ? defaultOptions.AnonymousGroupPrefix : "anonymous.";
            }

            base.PostProcess();
        }
    }
}
