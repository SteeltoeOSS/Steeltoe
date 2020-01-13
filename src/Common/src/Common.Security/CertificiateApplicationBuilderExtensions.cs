using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Steeltoe.Common.Security
{
    public static class CertificiateApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseCertificateRotation(this IApplicationBuilder applicationBuilder)
        {
            var certificateStoreService = applicationBuilder.ApplicationServices.GetService<ICertificateRotationService>();
            certificateStoreService.Start();
            return applicationBuilder;
        }
    }
}