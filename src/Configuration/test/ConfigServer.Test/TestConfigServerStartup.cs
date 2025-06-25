// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Steeltoe.Configuration.ConfigServer.Test;

internal sealed class TestConfigServerStartup : IDisposable
{
    private volatile CountdownEvent _firstRequestCountdownEvent = new(1);
    private volatile string? _response;
    private volatile int[] _returnStatus = [200];
    private volatile HttpRequestInfo? _lastRequest;
    private volatile int _requestCount;
    private volatile string _label = string.Empty;
    private volatile string _appName = string.Empty;
    private volatile string _env = string.Empty;

    public string? Response
    {
        get => _response;
        set => _response = value;
    }

    public int[] ReturnStatus
    {
        get => _returnStatus;
        set => _returnStatus = value;
    }

    public HttpRequestInfo? LastRequest
    {
        get => _lastRequest;
        set => _lastRequest = value;
    }

    public int RequestCount
    {
        get => _requestCount;
        set => _requestCount = value;
    }

    public string Label
    {
        get => _label;
        set => _label = value;
    }

    public string AppName
    {
        get => _appName;
        set => _appName = value;
    }

    public string Env
    {
        get => _env;
        set => _env = value;
    }

    public void Reset()
    {
        Response = null;
        ReturnStatus = [200];
        LastRequest = null;
        RequestCount = 0;
        Label = AppName = Env = string.Empty;

        _firstRequestCountdownEvent.Dispose();
        _firstRequestCountdownEvent = new CountdownEvent(1);
    }

    public void Configure(IApplicationBuilder app)
    {
        app.Run(async context =>
        {
            LastRequest = await HttpRequestInfo.CopyFromAsync(context);
            context.Response.StatusCode = GetStatusCode(context.Request.Path);
            RequestCount++;

            if (context.Response.StatusCode == 200)
            {
                context.Response.Headers.Append("content-type", "application/json");

                if (Response != null)
                {
                    await context.Response.WriteAsync(Response, context.RequestAborted);
                }
            }

            if (RequestCount == 1)
            {
                _firstRequestCountdownEvent.Signal();
            }
        });
    }

    private int GetStatusCode(string path)
    {
        if (!string.IsNullOrEmpty(Label) && !path.Contains(Label, StringComparison.Ordinal))
        {
            return 404;
        }

        if (!string.IsNullOrEmpty(Env) && !path.Contains(Env, StringComparison.Ordinal))
        {
            return 404;
        }

        if (!string.IsNullOrEmpty(AppName) && !path.Contains(AppName, StringComparison.Ordinal))
        {
            return 404;
        }

        return ReturnStatus[RequestCount];
    }

    public bool WaitForFirstRequest(TimeSpan timeout)
    {
        return _firstRequestCountdownEvent.Wait(timeout, TestContext.Current.CancellationToken);
    }

    public void Dispose()
    {
        _firstRequestCountdownEvent.Dispose();
    }
}
