// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Steeltoe.Management.Endpoint.ThreadDump;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.ThreadDump;

public sealed class ThreadDumpEndpointTest : BaseTest
{
    private readonly ITestOutputHelper _output;

    public ThreadDumpEndpointTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task Invoke_CallsDumpThreads()
    {
        using var testContext = new TestContext(_output);

        testContext.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<EventPipeThreadDumper>();
            services.AddThreadDumpActuatorServices(MediaTypeVersion.V1);
        };

        var handler = testContext.GetRequiredService<IThreadDumpEndpointHandler>();
        IList<ThreadInfo> result = await handler.InvokeAsync(null, CancellationToken.None);

        result.Should().NotBeEmpty();
    }
}
