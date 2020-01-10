using System;
using Microsoft.AspNetCore.Builder;
using Steeltoe.Common.Security;
using Steeltoe.Security.Authentication.MtlsCore;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryApplicationExtensions
    {
        public static IApplicationBuilder UseCloudFoundryContainerIdentity(this IApplicationBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.UseCertificateRotation();
            return app.UseMiddleware<CertificateForwarderMiddleware>();
        }
    }
}