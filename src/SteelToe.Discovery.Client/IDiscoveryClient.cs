//
// Copyright 2015 the original author or authors.
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


namespace SteelToe.Discovery.Client
{
    public interface IDiscoveryClient
    {

        /// <summary>
        /// A human readable description of the implementation
        /// </summary>
        string Description { get; }

        /// <summary>
        /// All known service Ids
        /// </summary>
        IList<string> Services { get; }

        /// <summary>
        ///  ServiceInstance with information used to register the local service
        /// </summary>
        /// <returns></returns>
        IServiceInstance GetLocalServiceInstance();

        /// <summary>
        ///  Get all ServiceInstances associated with a particular serviceId
        /// </summary>
        /// <param name="serviceId">the serviceId to lookup</param>
        /// <returns></returns>
        IList<IServiceInstance> GetInstances(String serviceId);


        void ShutdownAsync();


    }
}
