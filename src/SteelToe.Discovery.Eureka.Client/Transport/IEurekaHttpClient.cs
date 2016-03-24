using SteelToe.Discovery.Eureka.AppInfo;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SteelToe.Discovery.Eureka.Transport
{
    public interface IEurekaHttpClient
    {
        Task<EurekaHttpResponse> RegisterAsync(InstanceInfo info);

        Task<EurekaHttpResponse> CancelAsync(string appName, string id);

        Task<EurekaHttpResponse<InstanceInfo>> SendHeartBeatAsync(string appName, string id, InstanceInfo info, InstanceStatus overriddenStatus);

        Task<EurekaHttpResponse> StatusUpdateAsync(string appName, string id, InstanceStatus newStatus, InstanceInfo info);

        Task<EurekaHttpResponse> DeleteStatusOverrideAsync(string appName, string id, InstanceInfo info);

        Task<EurekaHttpResponse<Applications>> GetApplicationsAsync(ISet<string> regions = null);

        Task<EurekaHttpResponse<Applications>> GetDeltaAsync(ISet<string> regions = null);

        Task<EurekaHttpResponse<Applications>> GetVipAsync(string vipAddress, ISet<string> regions = null);

        Task<EurekaHttpResponse<Applications>> GetSecureVipAsync(string secureVipAddress, ISet<string> regions = null);

        Task<EurekaHttpResponse<Application>> GetApplicationAsync(string appName);

        Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string appName, string id);

        Task<EurekaHttpResponse<InstanceInfo>> GetInstanceAsync(string id);

        void Shutdown();
    }
}
