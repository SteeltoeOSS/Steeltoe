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

namespace Steeltoe.Stream.Binder
{
    /// <summary>
    /// Strategy for determining the partition to which a message should be sent.
    /// </summary>
    public interface IPartitionSelectorStrategy
    {
        /// <summary>
        /// Gets the name of the partition selector strategy
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Determine the partition based on a key. The partitionCount is 1 greater than the maximum value of a valid partition.
        /// </summary>
        /// <param name="key">the key</param>
        /// <param name="partitionCount">the number of partitions</param>
        /// <returns>the selected partition</returns>
        int SelectPartition(object key, int partitionCount);
    }
}
