// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.Discovery.Eureka.AppInfo;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Discovery.Eureka.Transport
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
