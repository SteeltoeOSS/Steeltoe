// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.TestResources;

namespace Steeltoe.Common.Test;

public sealed class PlatformTest
{
    [Fact]
    public void IsCloudFoundry_ReturnsExpected()
    {
        Assert.False(Platform.IsCloudFoundry);

        using (new EnvironmentVariableScope("VCAP_APPLICATION", "somevalue"))
        {
            Assert.True(Platform.IsCloudFoundry);
        }

        Assert.False(Platform.IsCloudFoundry);
    }

    [Fact]
    public void IsKubernetes_ReturnsExpected()
    {
        Assert.False(Platform.IsKubernetes);

        using (new EnvironmentVariableScope("KUBERNETES_SERVICE_HOST", "somevalue"))
        {
            Assert.True(Platform.IsKubernetes);
        }

        Assert.False(Platform.IsKubernetes);
    }

    [Fact]
    public void IsContainerized_ReturnsExpected()
    {
        Assert.False(Platform.IsContainerized);

        using (new EnvironmentVariableScope("DOTNET_RUNNING_IN_CONTAINER", "true"))
        {
            Assert.True(Platform.IsContainerized);
        }

        Assert.False(Platform.IsContainerized);
    }
}
