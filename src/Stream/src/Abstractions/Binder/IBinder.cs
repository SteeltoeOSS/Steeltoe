// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Stream.Config;
using System;

namespace Steeltoe.Stream.Binder
{
    /// <summary>
    /// A strategy interface used to bind an app interface to a logical name. The name is
    /// intended to identify a logical consumer or producer of messages. This may be a queue, a
    /// channel adapter, another message channel, etc.
    /// </summary>
    /// TODO:  Figure out Closable/Disposable usage
    public interface IBinder // : IDisposable
    {
        /// <summary>
        /// Gets the name of the binder
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the target type this binder can bind to
        /// </summary>
        Type TargetType { get; }

        /// <summary>
        /// Bind the target component as a message consumer to the logical entity identified by the name.
        /// </summary>
        /// <param name="name">the logical identity of the message source</param>
        /// <param name="group">the consumer group to which this consumer belongs</param>
        /// <param name="inboundTarget">the application interface to be bound as a consumer</param>
        /// <param name="consumerOptions">the consumer options</param>
        /// <returns>the setup binding</returns>
        IBinding BindConsumer(string name, string group, object inboundTarget, IConsumerOptions consumerOptions);

        /// <summary>
        /// Bind the target component as a message producer to the logical entity identified by the name.
        /// </summary>
        /// <param name="name">the logical identity of the message outbound target</param>
        /// <param name="outboundTarget">the application interface to be bound as a producer</param>
        /// <param name="producerOptions">the producer options</param>
        /// <returns>the setup binding</returns>
        IBinding BindProducer(string name, object outboundTarget, IProducerOptions producerOptions);
    }

    /// <summary>
    /// A typed version of the strategy interface used to bind an app interface to a logical name. The name is
    /// intended to identify a logical consumer or producer of messages. This may be a queue, a
    /// channel adapter, another message channel, etc.
    /// </summary>
    /// <typeparam name="T">the target type supported by the binder</typeparam>
    public interface IBinder<in T> : IBinder
    {
        /// <summary>
        /// Bind the target component as a message consumer to the logical entity identified by the name.
        /// </summary>
        /// <param name="name">the logical identity of the message source</param>
        /// <param name="group">the consumer group to which this consumer belongs</param>
        /// <param name="inboundTarget">the application interface to be bound as a consumer</param>
        /// <param name="consumerOptions">the consumer options</param>
        /// <returns>the setup binding</returns>
        IBinding BindConsumer(string name, string group, T inboundTarget, IConsumerOptions consumerOptions);

        /// <summary>
        /// Bind the target component as a message producer to the logical entity identified by the name.
        /// </summary>
        /// <param name="name">the logical identity of the message outbound target</param>
        /// <param name="outboundTarget">the application interface to be bound as a producer</param>
        /// <param name="producerOptions">the producer options</param>
        /// <returns>the setup binding</returns>
        IBinding BindProducer(string name, T outboundTarget, IProducerOptions producerOptions);
    }
}
