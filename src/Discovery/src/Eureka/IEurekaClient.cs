// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;
using System.Collections.Generic;
using T = System.Threading.Tasks;

namespace Steeltoe.Discovery.Eureka;

public interface IEurekaClient : ILookupService
{
    IList<InstanceInfo> GetInstancesByVipAddress(string vipAddress, bool secure);

    T.Task ShutdownAsync();
}
