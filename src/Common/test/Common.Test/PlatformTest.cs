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
        Platform.IsCloudFoundry.Should().BeFalse();

        using (new EnvironmentVariableScope("VCAP_APPLICATION", "{}"))
        {
            Platform.IsCloudFoundry.Should().BeTrue();
        }

        Platform.IsCloudFoundry.Should().BeFalse();
    }

    [Fact]
    public void IsKubernetes_ReturnsExpected()
    {
        Platform.IsKubernetes.Should().BeFalse();

        using (new EnvironmentVariableScope("KUBERNETES_SERVICE_HOST", "some"))
        {
            Platform.IsKubernetes.Should().BeTrue();
        }

        Platform.IsKubernetes.Should().BeFalse();
    }

    [Fact]
    public void IsContainerized_ReturnsExpected()
    {
        Platform.IsContainerized.Should().BeFalse();

        using (new EnvironmentVariableScope("DOTNET_RUNNING_IN_CONTAINER", "true"))
        {
            Platform.IsContainerized.Should().BeTrue();
        }

        Platform.IsContainerized.Should().BeFalse();
    }
}
