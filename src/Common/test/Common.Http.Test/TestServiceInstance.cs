// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Discovery;
using System;
using System.Collections.Generic;

namespace Steeltoe.Common.Http.Test;

internal class TestServiceInstance : IServiceInstance
{
    public TestServiceInstance(Uri uri)
    {
        Uri = uri;
    }

    public string Host
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public bool IsSecure
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public IDictionary<string, string> Metadata
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public int Port
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public string ServiceId
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public Uri Uri { get; private set; }
}