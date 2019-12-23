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

        string ApplicationId { get; }

        string ApplicationName { get; }

        /// <summary>
        /// Gets the name to use to represent the app instance in the context of a given Steeltoe component
        /// </summary>
        /// <param name="steeltoeComponent">The Steeltoe component requesting the app's name</param>
        /// <param name="additionalSearchPath">One additional config key to consider, used as top priority in search</param>
        /// <returns>The name of the application</returns>
        string ApplicationNameInContext(SteeltoeComponent steeltoeComponent, string additionalSearchPath = null);

        IEnumerable<string> ApplicationUris { get; }

        string ApplicationVersion { get; }

        int InstanceIndex { get; }

        int Port { get; }

        // string SpaceId { get; }
        // string SpaceName { get; }
        IEnumerable<string> Uris { get; }

        string Version { get; }

        int DiskLimit { get; }

        int MemoryLimit { get; }

        int FileDescriptorLimit { get; }

        string InstanceIP { get; }

        string InternalIP { get; }
    }
}
