using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryAuthorizationPolicyBuilderExtensions
    {
        public static AuthorizationPolicyBuilder SameOrg(this AuthorizationPolicyBuilder builder)
        {
            builder.Requirements.Add(new SameOrgRequirement());
            return builder;
        }

        public static AuthorizationPolicyBuilder SameSpace(this AuthorizationPolicyBuilder builder)
        {
            builder.Requirements.Add(new SameSpaceRequirement());
            return builder;
        }
    }
}