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

namespace Steeltoe.Management.Hypermedia
{
    /// <summary>
    /// Determines the Hypermedia actuator that will handle the context root and security.
    /// <example>Cloudfoundry root for Apps man integration at /cloudfoundryapplication
    /// Actuator root for usage outside cloudfoundry at /actuator (or configurable by management:endpoints:path
    /// </example>
    /// </summary>
    public enum ActuatorContext
    {
        CloudFoundry,
        Actuator,
        ActuatorAndCloudFoundry
    }
}
