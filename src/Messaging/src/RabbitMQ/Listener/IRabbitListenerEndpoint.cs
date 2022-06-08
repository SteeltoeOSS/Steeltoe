// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Batch;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public interface IRabbitListenerEndpoint
{
    string Id { get; set; }

    int? Concurrency { get; set; }

    bool? AutoStartup { get; set; }

    IApplicationContext ApplicationContext { get; set; }

    void SetupListenerContainer(IMessageListenerContainer messageListenerContainer);

    ISmartMessageConverter MessageConverter { get; set; }

    bool BatchListener { get; set; }

    IBatchingStrategy BatchingStrategy { get; set; }

    AcknowledgeMode? AckMode { get; set; }

    IReplyPostProcessor ReplyPostProcessor { get; set; }

    string Group { get; set; }
}
