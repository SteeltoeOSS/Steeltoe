// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.HeapDump;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.HeapDump;

public sealed class HeapDumpEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task Invoke_CreatesDump()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddHeapDumpActuator();
            services.AddSingleton<IHeapDumper, FakeHeapDumper>();
        };

        var handler = testContext.GetRequiredService<IHeapDumpEndpointHandler>();

        string path = await handler.InvokeAsync(null, TestContext.Current.CancellationToken);

        path.Should().NotBeNull();
        File.Exists(path).Should().BeTrue();
    }
}
