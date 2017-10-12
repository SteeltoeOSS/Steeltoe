//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Discovery
{
    public interface IServiceInstance
    {
        /// <summary>
        ///  The service id as register by the DiscoveryClient
        /// </summary>
        string ServiceId { get; }
        /// <summary>
        /// The hostname of the registered ServiceInstance
        /// </summary>
        string Host { get; }
        /// <summary>
        /// The port of the registered ServiceInstance
        /// </summary>
        int Port { get; }
        /// <summary>
        /// If the port of the registered ServiceInstance is https or not
        /// </summary>
        bool IsSecure { get; }
        /// <summary>
        /// the service uri address
        /// </summary>
        Uri Uri { get; }
        /// <summary>
        ///  The key value pair metadata associated with the service instance
        /// </summary>
        IDictionary<string, string> Metadata { get; }
    }
}
