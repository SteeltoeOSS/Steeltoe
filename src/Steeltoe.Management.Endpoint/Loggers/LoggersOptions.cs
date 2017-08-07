using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Loggers
{
    public class LoggersOptions : AbstractOptions, ILoggersOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:loggers";
        public LoggersOptions() : base()
        {
            Id = "loggers";
        }

        public LoggersOptions(IConfiguration config) :
             base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "loggers";
            }
        }

    }
}
