﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Stream.Binder
{
    /// <summary>
    /// An abstraction which defines a mechanism to poll a consumer
    /// </summary>
    public interface IPollableSource
    {
        /// <summary>
        /// Poll the consumer
        /// </summary>
        /// <param name="handler">the handler to process message</param>
        /// <returns>true if a message was handled</returns>
        bool Poll(object handler);

        /// <summary>
        /// Poll the consumer and convert the payload to the specified type. Throw a
        /// RequeueCurrentMessageException to force the current message to be requeued
        /// in the broker(after retries are exhausted, if configured).
        /// </summary>
        /// <param name="handler">the handler</param>
        /// <param name="type">the type of the payload</param>
        /// <returns>true if a message was handled</returns>
        bool Poll(object handler, Type type);
    }

    /// <summary>
    /// An abstraction which defines a mechanism to poll a consumer
    /// </summary>
    /// <typeparam name="H">the handler type</typeparam>
    public interface IPollableSource<in H> : IPollableSource
    {
        /// <summary>
        /// Poll the consumer
        /// </summary>
        /// <param name="handler">the handler to process message</param>
        /// <returns>true if a message was handled</returns>
        bool Poll(H handler);

        /// <summary>
        /// Poll the consumer and convert the payload to the specified type. Throw a
        /// RequeueCurrentMessageException to force the current message to be requeued
        /// in the broker(after retries are exhausted, if configured).
        /// </summary>
        /// <param name="handler">the handler</param>
        /// <param name="type">the type of the payload</param>
        /// <returns>true if a message was handled</returns>
        bool Poll(H handler, Type type);
    }
}
