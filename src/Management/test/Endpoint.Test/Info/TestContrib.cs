// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Info;

namespace Steeltoe.Management.Endpoint.Test.Info;

internal sealed class TestContrib : IInfoContributor
{
    public bool Called { get; private set; }
    public bool Throws { get; }

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
