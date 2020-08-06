﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Stream.Binder;

namespace Steeltoe.Stream.Partitioning
{
    public class CustomPartitionSelectorClass : IPartitionSelectorStrategy
    {
        public CustomPartitionSelectorClass()
        {
            ServiceName = this.GetType().Name;
        }

        public string ServiceName { get; set; }

        public int SelectPartition(object key, int partitionCount)
        {
            return int.Parse((string)key);
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
    public class CustomPartitionSelectorClassOne : IPartitionSelectorStrategy
    {
        public CustomPartitionSelectorClassOne()
        {
            ServiceName = this.GetType().Name;
        }

        public string ServiceName { get; set; }

        public int SelectPartition(object key, int partitionCount)
        {
            return int.Parse((string)key);
        }
    }

    public class CustomPartitionSelectorClassTwo : IPartitionSelectorStrategy
    {
        public CustomPartitionSelectorClassTwo()
        {
            ServiceName = this.GetType().Name;
        }

        public string ServiceName { get; set; }

        public int SelectPartition(object key, int partitionCount)
        {
            return int.Parse((string)key);
        }
    }
#pragma warning restore SA1402 // File may only contain a single type
}
