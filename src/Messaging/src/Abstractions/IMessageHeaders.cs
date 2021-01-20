﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

namespace Steeltoe.Messaging
{
    /// <summary>
    /// The headers for a message
    /// </summary>
#pragma warning disable S3444 // Interfaces should not simply inherit from base interfaces with colliding members
    public interface IMessageHeaders : IDictionary, System.Collections.Generic.IDictionary<string, object>
#pragma warning restore S3444 // Interfaces should not simply inherit from base interfaces with colliding members
    {
        /// <summary>
        /// Gets a header value given its key
        /// </summary>
        /// <typeparam name="T">the type of the value returned</typeparam>
        /// <param name="key">the name of the header</param>
        /// <returns>the value or null if not found</returns>
        T Get<T>(string key);

        /// <summary>
        /// Gets the ID header value
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the timestamp header value
        /// </summary>
        long? Timestamp { get; }

        /// <summary>
        /// Gets the reply channel the message is for
        /// </summary>
        object ReplyChannel { get; }

        /// <summary>
        /// Gets the error channel the message is for
        /// </summary>
        object ErrorChannel { get; }
    }
}
