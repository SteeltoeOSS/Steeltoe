// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;

namespace Steeltoe.Common.Http.Test;

internal sealed class TestServiceInstance : IServiceInstance
{
    public string Host => throw new NotImplementedException();

    public bool IsSecure => throw new NotImplementedException();

    public IDictionary<string, string> Metadata => throw new NotImplementedException();

    public int Port => throw new NotImplementedException();

    public string ServiceId => throw new NotImplementedException();

    public Uri Uri { get; }

    public TestServiceInstance(Uri uri)
    {
        Uri = uri;
    }
}
