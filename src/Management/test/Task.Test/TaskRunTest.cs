// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.TestResources;
using Xunit;

namespace Steeltoe.Management.Task.Test;

public sealed class TaskRunTest
{
    [Fact]
    public void DelegatingTask_WebHost_ExecutesRun()
    {
        string[] args =
        {
            "runtask=test"
        };

        IWebHost webHost = WebHost.CreateDefaultBuilder(args).UseDefaultServiceProvider(options => options.ValidateScopes = true)
            .UseStartup<TestServerStartup>().Build();

        Assert.Throws<PassException>(webHost.RunWithTasks);
    }

    [Fact]
    public void DelegatingTask_WebHost_StopsIfNoTask()
    {
        string[] args =
        {
            "runtask=test"
        };

        IWebHost webHost = WebHost.CreateDefaultBuilder(args).UseDefaultServiceProvider(options => options.ValidateScopes = true)
            .Configure(HostingHelpers.EmptyAction).Build();

        webHost.RunWithTasks();

        Assert.True(true, "If we reached this assertion, the app stopped without throwing anything");
    }

    [Fact]
    public void DelegatingTask_GenericHost_ExecutesRun()
    {
        string[] args =
        {
            "runtask=test"
        };

        IHost host = Host.CreateDefaultBuilder(args).UseDefaultServiceProvider(options => options.ValidateScopes = true)
            .ConfigureWebHost(configure => configure.UseStartup<TestServerStartup>().UseKestrel()).Build();

        Assert.Throws<PassException>(host.RunWithTasks);
    }

    [Fact]
    public void DelegatingTask_GenericHost_StopsIfNoTask()
    {
        string[] args =
        {
            "runtask=test"
        };

        Host.CreateDefaultBuilder(args).UseDefaultServiceProvider(options => options.ValidateScopes = true).Build().RunWithTasks();

        Assert.True(true, "If we reached this assertion, the app stopped without throwing anything");
    }
}
