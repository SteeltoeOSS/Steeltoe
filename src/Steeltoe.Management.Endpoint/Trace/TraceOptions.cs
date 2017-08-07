using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Trace
{
    public class TraceOptions : AbstractOptions, ITraceOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:trace";
        private const int DEFAULT_CAPACITY = 100;
        public TraceOptions() : base()
        {
            Id = "trace";
            Capacity = DEFAULT_CAPACITY;
        }

        public TraceOptions(IConfiguration config) :
             base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "trace";
            }
            if (Capacity == -1)
            {
                Capacity = DEFAULT_CAPACITY;
            }
        }

        public int Capacity { get; set; } = -1;
        public bool AddRequestHeaders { get; set; } = true;
        public bool AddResponseHeaders { get; set; } = true;
        public bool AddPathInfo { get; set; } = false;
        public bool AddUserPrincipal { get; set; } = false;
        public bool AddParameters { get; set; } = false;
        public bool AddQueryString { get; set; } = false;
        public bool AddAuthType { get; set; } = false;
        public bool AddRemoteAddress { get; set; } = false;
        public bool AddSessionId { get; set; } = false;
        public bool AddTimeTaken { get; set; } = true;
    }
}
