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

using Steeltoe.Common;
using System.IO;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Health.Contributor
{
    public class DiskSpaceContributor : IHealthContributor
    {
        private const string ID = "diskSpace";
        private readonly DiskSpaceContributorOptions _options;

        public DiskSpaceContributor(DiskSpaceContributorOptions options = null)
        {
            _options = options ?? new DiskSpaceContributorOptions();
        }

        public string Id { get; } = ID;

        public HealthCheckResult Health()
        {
            var result = new HealthCheckResult();

            var fullPath = Path.GetFullPath(_options.Path);
            var dirInfo = new DirectoryInfo(fullPath);
            if (dirInfo.Exists)
            {
                var rootName = dirInfo.Root.Name;
                var d = new DriveInfo(rootName);
                var freeSpace = d.TotalFreeSpace;
                if (freeSpace >= _options.Threshold)
                {
                    result.Status = HealthStatus.UP;
                }
                else
                {
                    result.Status = HealthStatus.DOWN;
                }

                result.Details.Add("total", d.TotalSize);
                result.Details.Add("free", freeSpace);
                result.Details.Add("threshold", _options.Threshold);
                result.Details.Add("status", result.Status.ToString());
            }

            return result;
        }
    }
}
