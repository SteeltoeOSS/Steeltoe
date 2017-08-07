using Microsoft.Extensions.Configuration;
using Steeltoe.Management.Endpoint.Security;
using System.Collections.Generic;

namespace Steeltoe.Management.Endpoint.Info
{
    public class InfoOptions : AbstractOptions, IInfoOptions
    {
        private const string MANAGEMENT_INFO_PREFIX = "management:endpoints:info";

        public InfoOptions() : base()
        {
            Id = "info";
            RequiredPermissions = Permissions.RESTRICTED;
        }

        public InfoOptions(IConfiguration config) :
             base(MANAGEMENT_INFO_PREFIX, config)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = "info";
            }

            if (RequiredPermissions == Permissions.UNDEFINED)
            {
                RequiredPermissions = Permissions.RESTRICTED;
            }
        }
    }
}
