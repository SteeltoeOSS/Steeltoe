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

using System.Collections.Generic;

namespace Steeltoe.Common
{
    public interface IApplicationInstanceInfo
    {
        string InstanceId { get; }

        /// <summary>
        /// Gets a GUID identifying the application
        /// </summary>
        string ApplicationId { get; }

        /// <summary>
        /// Gets the name of the application, as known to the hosting platform
        /// </summary>
        string ApplicationName { get; }

        /// <summary>
        /// Gets the URIs assigned to the app
        /// </summary>
        IEnumerable<string> Uris { get; }

        /// <summary>
        /// Gets the index number of the app instance
        /// </summary>
        int InstanceIndex { get; }

        /// <summary>
        /// Gets the port assigned to the app instance, on which it should listen
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Gets a GUID identifying the application instance
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the maximum amount of disk available for the app instance
        /// </summary>
        int DiskLimit { get; }

        /// <summary>
        /// Gets the maximum amount of memory available for the app instance
        /// </summary>
        int MemoryLimit { get; }

        /// <summary>
        /// Gets the maximum amount of file descriptors available to the app instance
        /// </summary>
        int FileDescriptorLimit { get; }

        /// <summary>
        /// Gets the internal IP address of the container running the app instance instance
        /// </summary>
        string InternalIP { get; }

        /// <summary>
        /// Gets the external IP address of the host running the app instance
        /// </summary>
        string InstanceIP { get; }

        /// <summary>
        /// Gets the name to use to represent the app instance in the context of a given Steeltoe component
        /// </summary>
        /// <param name="steeltoeComponent">The Steeltoe component requesting the app's name</param>
        /// <param name="additionalSearchPath">One additional config key to consider, used as top priority in search</param>
        /// <returns>The name of the application</returns>
        string ApplicationNameInContext(SteeltoeComponent steeltoeComponent, string additionalSearchPath = null);
    }
}
