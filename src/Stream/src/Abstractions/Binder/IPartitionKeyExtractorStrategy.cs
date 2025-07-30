// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;
using Steeltoe.Messaging;

namespace Steeltoe.Stream.Binder;

/// <summary>
/// Strategy for extracting a partition key from a Message.
/// </summary>
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IPartitionKeyExtractorStrategy : IServiceNameAware
{
    /// <summary>
    /// Extract the partition key from the incoming message
    /// </summary>
    /// <param name="message">the message to process</param>
    /// <returns>the key</returns>
    object ExtractKey(IMessage message);
}