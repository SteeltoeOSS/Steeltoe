// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.ThreadDump;
using Xunit;

namespace Steeltoe.Management.Endpoint.Test.ThreadDump;

public sealed class ServiceCollectionTests
{
    [Fact]
    public void AddThreadDumpActuatorServices_ThrowsOnNulls()
    {
        const IServiceCollection services = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddThreadDumpActuatorServices(MediaTypeVersion.V2));
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);
    }
}
