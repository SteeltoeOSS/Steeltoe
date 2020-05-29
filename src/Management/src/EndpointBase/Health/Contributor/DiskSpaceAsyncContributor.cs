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

using Steeltoe.Common.HealthChecks;
using System.IO;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Health.Contributor
{
    public class DiskSpaceAsyncContributor : DiskSpaceContributor, IAsyncHealthContributor
    {
        public DiskSpaceAsyncContributor(DiskSpaceContributorOptions options = null)
            : base(options)
        {
        }

        public async Task<HealthCheckResult> CheckHealthAsync()
        {
            return await Task.Run(() => Health());
        }
    }
}
