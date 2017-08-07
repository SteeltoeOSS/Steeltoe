using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Health.Contributor
{
    public class DiskSpaceContributorOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:health:diskspace";
        private const long DEFAULT_THRESHOLD = 10 * 1024 * 1024;
        public DiskSpaceContributorOptions() 
        {
            Path = ".";
            Threshold = DEFAULT_THRESHOLD;
        }

        public DiskSpaceContributorOptions(IConfiguration config) 
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            var section = config.GetSection(MANAGEMENT_INFO_PREFIX);
            if (section != null)
            {
                section.Bind(this);
            }
            if (string.IsNullOrEmpty(Path))
            {
                Path = ".";
            }
            if (Threshold == -1)
            {
                Threshold = DEFAULT_THRESHOLD;
            }
        }

        public string Path { get; set; }
        public long Threshold { get; set; } = -1;

    }
}
