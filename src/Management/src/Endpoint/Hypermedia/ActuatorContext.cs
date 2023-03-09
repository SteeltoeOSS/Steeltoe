// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Hypermedia;
internal class ActuatorContext :IContextName
{
    public const string Name = "Actuator";

    string IContextName.Name => Name;
}
