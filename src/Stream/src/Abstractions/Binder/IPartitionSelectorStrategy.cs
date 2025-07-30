// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Services;

namespace Steeltoe.Stream.Binder;

/// <summary>
/// Strategy for determining the partition to which a message should be sent.
/// </summary>
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IPartitionSelectorStrategy : IServiceNameAware
{
    /// <summary>
    /// Determine the partition based on a key. The partitionCount is 1 greater than the maximum value of a valid partition.
    /// </summary>
    /// <param name="key">the key</param>
    /// <param name="partitionCount">the number of partitions</param>
    /// <returns>the selected partition</returns>
    int SelectPartition(object key, int partitionCount);
}