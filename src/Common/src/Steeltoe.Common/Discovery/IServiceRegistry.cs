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

using System;

namespace Steeltoe.Common.Discovery
{
    /// <summary>
    /// Represents a Service registry (e.g. Consul, etc.)
    /// </summary>
    /// <typeparam name="T">a type representing a service instance</typeparam>
    public interface IServiceRegistry<T> : IDisposable
        where T : IServiceInstance
    {
        /// <summary>
        /// Register a service instance in the service registry
        /// </summary>
        /// <param name="registration">the service instance to register</param>
        void Register(T registration);

        /// <summary>
        /// Deregister a service instance in the service registry
        /// </summary>
        /// <param name="registration">the service instance to register</param>
        void Deregister(T registration);

        /// <summary>
        /// Update the registration in the service registry with the provided status
        /// </summary>
        /// <param name="registration">the registration to update</param>
        /// <param name="status">the status</param>
        void SetStatus(T registration, string status);

        /// <summary>
        /// Return the current status of the service registry registration
        /// </summary>
        /// <typeparam name="S">the status</typeparam>
        /// <param name="registration">the service registration to obtain status for</param>
        /// <returns>the returned status</returns>
        S GetStatus<S>(T registration)
            where S : class;
    }
}
