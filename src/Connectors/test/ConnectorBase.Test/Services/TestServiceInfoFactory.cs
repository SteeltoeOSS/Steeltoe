// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration;
using System;

namespace Steeltoe.Connector.Services.Test;

internal class TestServiceInfoFactory : ServiceInfoFactory
{
    public TestServiceInfoFactory(Tags tags, string scheme)
        : base(tags, scheme)
    {
    }

    public TestServiceInfoFactory(Tags tags, string[] schemes)
        : base(tags, schemes)
    {
    }

    public override IServiceInfo Create(Service binding)
    {
        throw new NotImplementedException();
    }
}
