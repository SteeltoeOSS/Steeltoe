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

namespace Steeltoe.Stream.Provisioning
{
    /// <summary>
    /// Represents a ConsumerDestination that provides the information about the destination
    /// that is physically provisioned through a provisioning provider
    /// </summary>
    public interface IConsumerDestination
    {
        /// <summary>
        /// Gets the destination name
        /// </summary>
        string Name { get; }
    }
}
