// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Formats.Asn1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Endpoint.Test.Infrastructure;
using Steeltoe.Management.Endpoint.ThreadDump;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.Management.Endpoint.Test.ThreadDump;

public class ThreadDumpEndpointTest : BaseTest
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
        Assert.Throws<ArgumentNullException>(() => new ThreadDumpEndpoint(options, null, NullLoggerFactory.Instance));
    }

    [Fact]
    public async Task Invoke_CallsDumpThreads()
    {
        using var tc = new TestContext(_output);
        var dumper = new TestThreadDumper();

        tc.AdditionalServices = (services, configuration) =>
        {
            services.AddSingleton<IThreadDumper>(dumper);
            services.AddThreadDumpActuatorServices(MediaTypeVersion.V1);
        };

        var ep = tc.GetService<IThreadDumpEndpoint>();
        IList<ThreadInfo> result = await ep.InvokeAsync(CancellationToken.None);
        Assert.NotNull(result);
        Assert.True(dumper.DumpThreadsCalled);
    }
}
