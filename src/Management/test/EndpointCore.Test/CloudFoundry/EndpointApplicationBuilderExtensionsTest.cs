// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Steeltoe.Management.Endpoint.Test;
using System;
using Xunit;

namespace Steeltoe.Management.Endpoint.CloudFoundry.Test;

public class EndpointApplicationBuilderExtensionsTest : BaseTest
{
    [Fact]
    public void UseCloudFoundrySecurity_ThrowsIfNulls()
    {
        const IApplicationBuilder builder = null;

        Assert.Throws<ArgumentNullException>(() => builder.UseCloudFoundrySecurity());
    }
}
