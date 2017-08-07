//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.IO;

namespace Steeltoe.Management.Endpoint.Health.Contributor
{
    public class DiskSpaceContributor : IHealthContributor
    {
        private DiskSpaceContributorOptions _options;
        private const string ID = "diskSpace";

        public DiskSpaceContributor(DiskSpaceContributorOptions options = null)
        {
            if (options == null)
            {
                _options = new DiskSpaceContributorOptions();
            } else
            {
                _options = options;
            }
   
        }

        public string Id { get; } = ID;
   

        public Health Health()
        {
            Health result = new Health();

            var fullPath = Path.GetFullPath(_options.Path);
            DirectoryInfo dirInfo = new DirectoryInfo(fullPath);
            if (dirInfo.Exists)
            {
                string rootName = dirInfo.Root.Name;
                DriveInfo d = new DriveInfo(rootName);
                var freeSpace = d.TotalFreeSpace;
                if (freeSpace >= _options.Threshold)
                {
                    result.Status = HealthStatus.UP;
                } else
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
