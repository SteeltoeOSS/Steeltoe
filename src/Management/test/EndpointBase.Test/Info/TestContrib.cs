// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Info;
using System;

namespace Steeltoe.Management.Endpoint.Info.Test;

internal class TestContrib : IInfoContributor
{
    public bool Called;
    public bool Throws;

    public TestContrib()
    {
        Throws = false;
    }

    public TestContrib(bool throws)
    {
        Throws = throws;
    }

    public void Contribute(IInfoBuilder builder)
    {
        if (Throws)
        {
            throw new Exception();
        }

        Called = true;
    }
}
