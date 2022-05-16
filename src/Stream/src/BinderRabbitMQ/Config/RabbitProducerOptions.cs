// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.RabbitMQ.Core;
using System.Collections.Generic;

namespace Steeltoe.Stream.Binder.Rabbit.Config
{
    public class RabbitProducerOptions : RabbitCommonOptions
    {
        public bool? Compress { get; set; }

        public bool? BatchingEnabled { get; set; }

        public int? BatchSize { get; set; }

        public int? BatchBufferLimit { get; set; }

        public int? BatchTimeout { get; set; }

        // Do we need this?
        public bool? DurableSubscription { get; set; }

        public bool? Exclusive { get; set; }

        public long? FailedDeclarationRetryInterval { get; set; }

        // end
        public bool? Transacted { get; set; }

        public MessageDeliveryMode? DeliveryMode { get; set; }

        public List<string> HeaderPatterns { get; set; }

        public string DelayExpression { get; set; }

        public string RoutingKeyExpression { get; set; }

        public string ConfirmAckChannel { get; set; }

        public bool? UseConfirmHeader { get; set; }

        internal void PostProcess(RabbitProducerOptions defaultOptions = null)
        {
            if (!Compress.HasValue)
            {
                Compress = defaultOptions != null ? defaultOptions.Compress : false;
            }

            if (!BatchingEnabled.HasValue)
            {
                BatchingEnabled = defaultOptions?.BatchingEnabled ?? false;
            }

            if (!BatchSize.HasValue)
            {
                BatchSize = defaultOptions?.BatchSize ?? 100;
            }

            if (!BatchBufferLimit.HasValue)
            {
                BatchBufferLimit = defaultOptions?.BatchBufferLimit ?? 10000;
            }

            if (!BatchTimeout.HasValue)
            {
                BatchTimeout = defaultOptions?.BatchTimeout ?? 5000;
            }

            if (!Transacted.HasValue)
            {
                Transacted = defaultOptions?.Transacted ?? false;
            }

            if (!DeliveryMode.HasValue)
            {
                DeliveryMode = defaultOptions?.DeliveryMode ?? MessageDeliveryMode.PERSISTENT;
            }

            if (HeaderPatterns == null)
            {
                HeaderPatterns = defaultOptions?.HeaderPatterns ?? new List<string>() { "*" };
            }

            if (DelayExpression == null)
            {
                DelayExpression = defaultOptions?.DelayExpression;
            }

            if (RoutingKeyExpression == null)
            {
                RoutingKeyExpression = defaultOptions?.RoutingKeyExpression;
            }

            if (ConfirmAckChannel == null)
            {
                ConfirmAckChannel = defaultOptions?.ConfirmAckChannel;
            }

            if (!UseConfirmHeader.HasValue)
            {
                UseConfirmHeader = defaultOptions?.UseConfirmHeader ?? false;
            }

            base.PostProcess(defaultOptions);
        }
    }
}
