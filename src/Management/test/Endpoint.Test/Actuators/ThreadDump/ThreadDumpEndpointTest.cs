// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Actuators.ThreadDump;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.Actuators.ThreadDump;

public sealed class ThreadDumpEndpointTest(ITestOutputHelper testOutputHelper) : BaseTest
{
    private readonly ITestOutputHelper _testOutputHelper = testOutputHelper;

    [Fact]
    public async Task Invoke_CallsDumpThreads()
    {
        using var testContext = new SteeltoeTestContext(_testOutputHelper);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<IThreadDumper, FakeThreadDumper>();
            services.AddThreadDumpActuator();
        };

        var handler = testContext.GetRequiredService<IThreadDumpEndpointHandler>();
        IList<ThreadInfo> result = await handler.InvokeAsync(null, TestContext.Current.CancellationToken);

        result.Should().NotBeEmpty();
    }
}
