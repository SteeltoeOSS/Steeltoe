// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;
using Steeltoe.Stream.Config;

namespace Steeltoe.Stream.Binder;

/// <summary>
/// A strategy interface used to bind an app interface to a logical name. The name is
/// intended to identify a logical consumer or producer of messages. This may be a queue, a
/// channel adapter, another message channel, etc.
/// </summary>
public interface IBinder : IServiceNameAware, IDisposable
{
    /// <summary>
    /// Gets the target type this binder can bind to.
    /// </summary>
    Type TargetType { get; }

    /// <summary>
    /// Bind the target component as a message consumer to the logical entity identified by the name.
    /// </summary>
    /// <param name="name">the logical identity of the message source.</param>
    /// <param name="group">the consumer group to which this consumer belongs.</param>
    /// <param name="inboundTarget">the application interface to be bound as a consumer.</param>
    /// <param name="consumerOptions">the consumer options.</param>
    /// <returns>the setup binding.</returns>
    IBinding BindConsumer(string name, string group, object inboundTarget, IConsumerOptions consumerOptions);

    /// <summary>
    /// Bind the target component as a message producer to the logical entity identified by the name.
    /// </summary>
    /// <param name="name">the logical identity of the message outbound target.</param>
    /// <param name="outboundTarget">the application interface to be bound as a producer.</param>
    /// <param name="producerOptions">the producer options.</param>
    /// <returns>the setup binding.</returns>
    IBinding BindProducer(string name, object outboundTarget, IProducerOptions producerOptions);
}

/// <summary>
/// A typed version of the strategy interface used to bind an app interface to a logical name. The name is
/// intended to identify a logical consumer or producer of messages. This may be a queue, a
/// channel adapter, another message channel, etc.
/// </summary>
/// <typeparam name="T">the target type supported by the binder.</typeparam>
public interface IBinder<in T> : IBinder
{
    /// <summary>
    /// Bind the target component as a message consumer to the logical entity identified by the name.
    /// </summary>
    /// <param name="name">the logical identity of the message source.</param>
    /// <param name="group">the consumer group to which this consumer belongs.</param>
    /// <param name="inboundTarget">the application interface to be bound as a consumer.</param>
    /// <param name="consumerOptions">the consumer options.</param>
    /// <returns>the setup binding.</returns>
    IBinding BindConsumer(string name, string group, T inboundTarget, IConsumerOptions consumerOptions);

    /// <summary>
    /// Bind the target component as a message producer to the logical entity identified by the name.
    /// </summary>
    /// <param name="name">the logical identity of the message outbound target.</param>
    /// <param name="outboundTarget">the application interface to be bound as a producer.</param>
    /// <param name="producerOptions">the producer options.</param>
    /// <returns>the setup binding.</returns>
    IBinding BindProducer(string name, T outboundTarget, IProducerOptions producerOptions);
}
