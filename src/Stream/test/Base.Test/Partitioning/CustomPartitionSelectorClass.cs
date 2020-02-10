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
