// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;

namespace Steeltoe.Connector.Services.Test;

internal class TestServiceInfo : ServiceInfo
{
    public TestServiceInfo(string id, IApplicationInstanceInfo info)
        : base(id, info)
    {
    }

    public TestServiceInfo(string id)
        : base(id, null)
    {
    }
}
