﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Binder;

namespace Steeltoe.Stream.Partitioning
{
    public class CustomPartitionSelectorClass : IPartitionSelectorStrategy
    {
        public string Name => this.GetType().Name;

        public int SelectPartition(object key, int partitionCount)
        {
            return int.Parse((string)key);
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
    public class CustomPartitionSelectorClassOne : IPartitionSelectorStrategy
    {
        public string Name => this.GetType().Name;

        public int SelectPartition(object key, int partitionCount)
        {
            return int.Parse((string)key);
        }
    }

    public class CustomPartitionSelectorClassTwo : IPartitionSelectorStrategy
    {
        public string Name => this.GetType().Name;

        public int SelectPartition(object key, int partitionCount)
        {
            return int.Parse((string)key);
        }
    }
#pragma warning restore SA1402 // File may only contain a single type
}
