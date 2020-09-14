// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging.Rabbit.Core;
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

        public bool? Transacted { get; set; }

        public MessageDeliveryMode? DeliveryMode { get; set; }

        public List<string> HeaderPatterns { get; set; }

        public string DelayExpression { get; set; }

        public string RoutingKeyExpression { get; set; }

        public string ConfirmAckChannel { get; set; }

        internal void PostProcess(RabbitProducerOptions defaultOptions = null)
        {
            if (!Compress.HasValue)
            {
                Compress = defaultOptions != null ? defaultOptions.Compress : false;
            }

            if (!BatchingEnabled.HasValue)
            {
                BatchingEnabled = defaultOptions != null ? defaultOptions.BatchingEnabled : false;
            }

            if (!BatchSize.HasValue)
            {
                BatchSize = defaultOptions != null ? defaultOptions.BatchSize : 100;
            }

            if (!BatchBufferLimit.HasValue)
            {
                BatchBufferLimit = defaultOptions != null ? defaultOptions.BatchBufferLimit : 10000;
            }

            if (!BatchTimeout.HasValue)
            {
                BatchTimeout = defaultOptions != null ? defaultOptions.BatchTimeout : 5000;
            }

            if (!Transacted.HasValue)
            {
                Transacted = defaultOptions != null ? defaultOptions.Transacted : false;
            }

            if (!DeliveryMode.HasValue)
            {
                DeliveryMode = defaultOptions != null ? defaultOptions.DeliveryMode : MessageDeliveryMode.PERSISTENT;
            }

            if (HeaderPatterns == null)
            {
                HeaderPatterns = defaultOptions != null ? defaultOptions.HeaderPatterns : new List<string>() { "*" };
            }

            if (DelayExpression == null)
            {
                DelayExpression = defaultOptions != null ? defaultOptions.DelayExpression : null;
            }

            if (RoutingKeyExpression == null)
            {
                RoutingKeyExpression = defaultOptions != null ? defaultOptions.RoutingKeyExpression : null;
            }

            if (ConfirmAckChannel == null)
            {
                ConfirmAckChannel = defaultOptions != null ? defaultOptions.ConfirmAckChannel : null;
            }
        }
    }
}
