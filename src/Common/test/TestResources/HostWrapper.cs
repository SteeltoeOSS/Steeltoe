// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Steeltoe.Common.TestResources;

public sealed class HostWrapper : IAsyncDisposable
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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _host?.StartAsync(cancellationToken) ?? _webHost?.StartAsync(cancellationToken) ??
            _webApplication?.StartAsync(cancellationToken) ?? throw new NotSupportedException();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _host?.StopAsync(cancellationToken) ??
            _webHost?.StopAsync(cancellationToken) ?? _webApplication?.StopAsync(cancellationToken) ?? throw new NotSupportedException();
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(_host);
        await DisposeAsync(_webHost);
        await DisposeAsync(_webApplication);
    }

    private static async ValueTask DisposeAsync(IDisposable? disposable)
    {
        if (disposable is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            disposable?.Dispose();
        }
    }
}
