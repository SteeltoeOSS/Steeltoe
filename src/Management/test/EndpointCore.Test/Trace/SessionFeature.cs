// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Steeltoe.Management.Endpoint.Trace.Test;

internal class SessionFeature : ISessionFeature
{
    public ISession Session { get; set; }
}