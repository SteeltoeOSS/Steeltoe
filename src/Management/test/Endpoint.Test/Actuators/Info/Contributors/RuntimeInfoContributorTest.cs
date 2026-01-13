// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Steeltoe.Common.TestResources;
using Steeltoe.Management.Endpoint.Actuators.Info;
using Steeltoe.Management.Endpoint.Actuators.Info.Contributors;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Info.Contributors;

public sealed class RuntimeInfoContributorTest
{
    [Fact]
    public async Task Contributes_runtime_information()
    {
        var contributor = new RuntimeInfoContributor();
        var infoBuilder = new InfoBuilder();

        await contributor.ContributeAsync(infoBuilder, TestContext.Current.CancellationToken);

        IDictionary<string, object?> data = infoBuilder.Build();
        data.Should().ContainKey("runtime");

        var runtimeInfo = data["runtime"] as Dictionary<string, string?>;
        runtimeInfo.Should().NotBeNull();
        runtimeInfo.Should().ContainKey("name");
        runtimeInfo.Should().ContainKey("version");
        runtimeInfo.Should().ContainKey("runtimeIdentifier");

        runtimeInfo!["name"].Should().Be(RuntimeInformation.FrameworkDescription);
        runtimeInfo["version"].Should().Be(System.Environment.Version.ToString());
        runtimeInfo["runtimeIdentifier"].Should().Be(RuntimeInformation.RuntimeIdentifier);
    }

    [Fact]
    public async Task Does_not_throw_when_builder_is_null()
    {
        var contributor = new RuntimeInfoContributor();

        Func<Task> action = async () => await contributor.ContributeAsync(null!, TestContext.Current.CancellationToken);

        await action.Should().ThrowAsync<ArgumentNullException>();
    }
}
