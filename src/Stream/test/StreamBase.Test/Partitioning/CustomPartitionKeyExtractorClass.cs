// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using Steeltoe.Stream.Binder;

namespace Steeltoe.Stream.Partitioning
{
    public class CustomPartitionKeyExtractorClass : IPartitionKeyExtractorStrategy
    {
        public CustomPartitionKeyExtractorClass()
        {
            ServiceName = this.GetType().Name;
        }

        public string ServiceName { get; set; }

        public object ExtractKey(IMessage message)
        {
            return message.Headers.Get<string>("key");
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
    public class CustomPartitionKeyExtractorClassOne : IPartitionKeyExtractorStrategy
    {
        public CustomPartitionKeyExtractorClassOne()
        {
            ServiceName = this.GetType().Name;
        }

        public string ServiceName { get; set; }

        public object ExtractKey(IMessage message)
        {
            return message.Headers.Get<string>("key");
        }
    }

    public class CustomPartitionKeyExtractorClassTwo : IPartitionKeyExtractorStrategy
    {
        public CustomPartitionKeyExtractorClassTwo()
        {
            ServiceName = this.GetType().Name;
        }

        public string ServiceName { get; set; }

        public object ExtractKey(IMessage message)
        {
            return message.Headers.Get<string>("key");
        }
    }
#pragma warning restore SA1402 // File may only contain a single type
}
