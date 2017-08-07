using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
