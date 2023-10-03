// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Discovery.Eureka.AppInfo;

namespace Steeltoe.Discovery.Eureka.Transport;

public interface IEurekaHttpClient
{
    Task<EurekaHttpResponse> RegisterAsync(InstanceInfo info, CancellationToken cancellationToken);

    Task<EurekaHttpResponse> CancelAsync(string appName, string id, CancellationToken cancellationToken);

    Task<EurekaHttpResponse<InstanceInfo>> SendHeartBeatAsync(string appName, string id, InstanceInfo info, InstanceStatus overriddenStatus,
        CancellationToken cancellationToken);

    Task<EurekaHttpResponse> StatusUpdateAsync(string appName, string id, InstanceStatus newStatus, InstanceInfo info, CancellationToken cancellationToken);

    Task<EurekaHttpResponse> DeleteStatusOverrideAsync(string appName, string id, InstanceInfo info, CancellationToken cancellationToken);

    Task<EurekaHttpResponse<Applications>> GetApplicationsAsync(CancellationToken cancellationToken);

    Task<EurekaHttpResponse<Applications>> GetApplicationsAsync(ISet<string> regions, CancellationToken cancellationToken);

    Task<EurekaHttpResponse<Applications>> GetDeltaAsync(CancellationToken cancellationToken);

    Task<EurekaHttpResponse<Applications>> GetDeltaAsync(ISet<string> regions, CancellationToken cancellationToken);

    Task<EurekaHttpResponse<Applications>> GetVipAsync(string vipAddress, CancellationToken cancellationToken);

    Task<EurekaHttpResponse<Applications>> GetVipAsync(string vipAddress, ISet<string> regions, CancellationToken cancellationToken);

    Task<EurekaHttpResponse<Applications>> GetSecureVipAsync(string secureVipAddress, CancellationToken cancellationToken);

    Task<EurekaHttpResponse<Applications>> GetSecureVipAsync(string secureVipAddress, ISet<string> regions, CancellationToken cancellationToken);

    Task<EurekaHttpResponse<Application>> GetApplicationAsync(string appName, CancellationToken cancellationToken);

    Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string appName, string id, CancellationToken cancellationToken);

    Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string id, CancellationToken cancellationToken);

    Task ShutdownAsync(CancellationToken cancellationToken);
}
