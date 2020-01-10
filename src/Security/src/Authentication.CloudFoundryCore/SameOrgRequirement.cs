using Microsoft.AspNetCore.Authorization;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public class SameOrgRequirement : IAuthorizationRequirement
    {
    }
}