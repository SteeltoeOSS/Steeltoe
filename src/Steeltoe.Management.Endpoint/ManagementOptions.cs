using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Management.Endpoint
{
    public class ManagementOptions : IManagementOptions
    {
        private const string DEFAULT_PATH = "/";
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints";
        public bool Enabled { get; set; }

        public bool Sensitive { get; set; }

        public string Path { get; set; }

        public List<IEndpointOptions> EndpointOptions { get; set; }

        internal ManagementOptions() 
        {
            Enabled = true;
            Sensitive = false;
            Path = DEFAULT_PATH;
            EndpointOptions = new List<IEndpointOptions>();
        }

        internal ManagementOptions(IConfiguration config) : this()
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
        }

        internal static ManagementOptions _instance;

        public static ManagementOptions GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ManagementOptions();
            }
            return _instance;
        }
        public static ManagementOptions GetInstance(IConfiguration config)
        {
            if (_instance == null)
            {
                _instance = new ManagementOptions(config);
            }
            return _instance;
        }

    }
}
