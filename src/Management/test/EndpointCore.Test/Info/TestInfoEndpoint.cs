// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Management.Info;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Info.Test;

internal class TestInfoEndpoint : InfoEndpoint
{
    public TestInfoEndpoint(IInfoOptions options, IEnumerable<IInfoContributor> contributors, ILogger<InfoEndpoint> logger = null)
        : base(options, contributors, logger)
    {
    }

    public override Dictionary<string, object> Invoke()
    {
        return new Dictionary<string, object>();
    }
}
