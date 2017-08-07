using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Security;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Health
{
    public class HealthOptions : AbstractOptions, IHealthOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:health";

        public HealthOptions() : base()
        {
            Id = "health";
            RequiredPermissions = Permissions.RESTRICTED;
        }

        public HealthOptions(IConfiguration config) :
             base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "health";
            }

            if (RequiredPermissions == Permissions.UNDEFINED)
            {
                RequiredPermissions = Permissions.RESTRICTED;
            }

        }
    }
}
