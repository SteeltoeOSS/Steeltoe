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

using Steeltoe.Common.Discovery;
using System.Threading.Tasks;

namespace Steeltoe.Discovery.Consul.Registry
{
    /// <summary>
    /// A Consul service registry
    /// </summary>
    public interface IConsulServiceRegistry : IServiceRegistry<IConsulRegistration>
    {
        /// <summary>
        /// Register the provided registration in Consul
        /// </summary>
        /// <param name="registration">the registration to register</param>
        /// <returns>the task</returns>
        Task RegisterAsync(IConsulRegistration registration);

        /// <summary>
        /// Deregister the provided registration in Consul
        /// </summary>
        /// <param name="registration">the registration to register</param>
        /// <returns>the task</returns>
        Task DeregisterAsync(IConsulRegistration registration);

        /// <summary>
        /// Set the status of the registration in Consul
        /// </summary>
        /// <param name="registration">the registration to register</param>
        /// <param name="status">the status value to set</param>
        /// <returns>the task</returns>
        Task SetStatusAsync(IConsulRegistration registration, string status);

        /// <summary>
        /// Get the status of the registration in Consul
        /// </summary>
        /// <param name="registration">the registration to register</param>
        /// <returns>the status value</returns>
        Task<object> GetStatusAsync(IConsulRegistration registration);
    }
}
