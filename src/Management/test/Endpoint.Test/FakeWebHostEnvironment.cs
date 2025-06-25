// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Steeltoe.Management.Endpoint.Test;

internal sealed class FakeWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public IFileProvider ContentRootFileProvider
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public string ContentRootPath
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public string EnvironmentName
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public string WebRootPath
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public IFileProvider WebRootFileProvider
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
}
