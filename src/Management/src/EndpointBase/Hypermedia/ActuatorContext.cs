// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
        ActuatorAndCloudFoundry,
    }
}
