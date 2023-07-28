// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#pragma warning disable S4004 // Collection properties should be readonly

namespace Steeltoe.Management.Endpoint.Environment;

public sealed class EnvironmentEndpointOptions : EndpointOptions
{
    public IList<string> KeysToSanitize { get; set; }
}
