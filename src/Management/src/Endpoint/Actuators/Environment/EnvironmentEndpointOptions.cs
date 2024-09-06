// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Configuration;

namespace Steeltoe.Management.Endpoint.Actuators.Environment;

public sealed class EnvironmentEndpointOptions : EndpointOptions
{
    /// <summary>
    /// Gets the list of keys to sanitize. Allows regular expressions.
    /// </summary>
    public IList<string> KeysToSanitize { get; } = new List<string>();
}
