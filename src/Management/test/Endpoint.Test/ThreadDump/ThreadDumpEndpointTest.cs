// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Steeltoe.Management.Endpoint.ThreadDump;
using Xunit;
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
    public void Constructor_ThrowsIfNullRepo()
    {
        IOptionsMonitor<ThreadDumpEndpointOptions> options = GetOptionsMonitorFromSettings<ThreadDumpEndpointOptions>();
        Assert.Throws<ArgumentNullException>(() => new ThreadDumpEndpointHandler(options, null, NullLoggerFactory.Instance));
    }

    [Fact]
    public async Task Invoke_CallsDumpThreads()
    {
        using var tc = new TestContext(_output);

        tc.AdditionalServices = (services, _) =>
        {
            services.AddSingleton<EventPipeThreadDumper>();
            services.AddThreadDumpActuatorServices(MediaTypeVersion.V1);
        };

        var ep = tc.GetRequiredService<IThreadDumpEndpointHandler>();
        IList<ThreadInfo> result = await ep.InvokeAsync(null, CancellationToken.None);

        result.Should().NotBeEmpty();
    }
}
