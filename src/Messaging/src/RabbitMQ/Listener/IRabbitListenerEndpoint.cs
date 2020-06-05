// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Rabbit.Batch;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Listener.Adapters;
using Steeltoe.Messaging.Rabbit.Support.Converter;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public interface IRabbitListenerEndpoint
    {
        string Id { get; set; }

        int? Concurrency { get; set; }

        bool? AutoStartup { get; set; }

        IApplicationContext ApplicationContext { get; set; }

        void SetupListenerContainer(IMessageListenerContainer messageListenerContainer);

        IMessageConverter MessageConverter { get; set; }

        bool BatchListener { get; set; }

        IBatchingStrategy BatchingStrategy { get; set; }

        AcknowledgeMode? AckMode { get; set; }

        IReplyPostProcessor ReplyPostProcessor { get; set; }
    }
}
