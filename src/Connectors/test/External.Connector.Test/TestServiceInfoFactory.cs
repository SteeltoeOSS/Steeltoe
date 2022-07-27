// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Connector.Services;
using Steeltoe.Extensions.Configuration;

namespace External.Connector.Test;

internal class TestServiceInfoFactory : ServiceInfoFactory
{
    public TestServiceInfoFactory()
        : base(new Tags("test"), "test")
    {
    }

    public override bool Accepts(Service binding)
    {
        return true;
    }

    public override IServiceInfo Create(Service binding)
    {
        return new DB2ServiceInfo("test", "test://test/test");
    }
}