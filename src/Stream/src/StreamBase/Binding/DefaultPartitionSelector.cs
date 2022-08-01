// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Binder;

namespace Steeltoe.Stream.Binding;

internal sealed class DefaultPartitionSelector : IPartitionSelectorStrategy
{
    public string ServiceName { get; set; } = "DefaultPartitionSelector";

    public int SelectPartition(object key, int partitionCount)
    {
        var hashcode = key.GetHashCode();
        if (hashcode == int.MinValue)
        {
            hashcode = 0;
        }

        return Math.Abs(hashcode);
    }
}
