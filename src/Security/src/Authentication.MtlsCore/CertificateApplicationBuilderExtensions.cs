// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Security;

namespace Steeltoe.Security.Authentication.Mtls;

public static class CertificateApplicationBuilderExtensions
{
    /// <summary>
    /// Start the certificate rotation service.
    /// </summary>
    /// <param name="applicationBuilder">The <see cref="ApplicationBuilder"/>.</param>
    [Obsolete("This functionality has moved to CertificateRotationService, this method will be removed in a future release")]
    public static IApplicationBuilder UseCertificateRotation(this IApplicationBuilder applicationBuilder)
    {
        var certificateStoreService = applicationBuilder.ApplicationServices.GetService<ICertificateRotationService>();
        certificateStoreService.Start();
        return applicationBuilder;
    }
}
