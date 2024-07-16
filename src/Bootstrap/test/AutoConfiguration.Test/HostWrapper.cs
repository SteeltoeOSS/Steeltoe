// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Bootstrap.AutoConfiguration.Test;

internal sealed class HostWrapper : IAsyncDisposable
{
    private readonly IHost? _host;
    private readonly IWebHost? _webHost;
    private readonly WebApplication? _webApplication;

    public IServiceProvider Services => _host?.Services ?? _webHost?.Services ?? _webApplication?.Services ?? throw new NotSupportedException();

    private HostWrapper(IHost host)
    {
        _host = host;
    }

    private HostWrapper(IWebHost webHost)
    {
        _webHost = webHost;
    }

    private HostWrapper(WebApplication webApplication)
    {
        _webApplication = webApplication;
    }

    public static HostWrapper Wrap(IHost host)
    {
        return new HostWrapper(host);
    }

    public static HostWrapper Wrap(IWebHost host)
    {
        return new HostWrapper(host);
    }

    public static HostWrapper Wrap(WebApplication host)
    {
        return new HostWrapper(host);
    }

    public HttpClient GetTestClient()
    {
        return _host?.GetTestClient() ?? _webHost?.GetTestClient() ?? _webApplication?.GetTestClient() ?? throw new NotSupportedException();
    }

    public Task StartAsync()
    {
        return _host?.StartAsync() ?? _webHost?.StartAsync() ?? _webApplication?.StartAsync() ?? throw new NotSupportedException();
    }

    public async ValueTask DisposeAsync()
    {
        _host?.Dispose();
        _webHost?.Dispose();

        if (_webApplication != null)
        {
            await _webApplication.DisposeAsync();
        }
    }
}
