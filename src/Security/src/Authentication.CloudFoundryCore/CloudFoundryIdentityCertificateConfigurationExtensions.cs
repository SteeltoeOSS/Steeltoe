using System;
using Microsoft.Extensions.Configuration;
using Steeltoe.Common.Security;

namespace Steeltoe.Security.Authentication.CloudFoundry
{
    public static class CloudFoundryIdentityCertificateConfigurationExtensions
    {
        public static IConfigurationBuilder AddCloudFoundryContainerIdentity(this IConfigurationBuilder builder)
        {
            var certFile = Environment.GetEnvironmentVariable("CF_INSTANCE_CERT");
            var keyFile = Environment.GetEnvironmentVariable("CF_INSTANCE_KEY");
            if (certFile == null || keyFile == null)
                return builder;
            return builder.AddPemFiles(certFile, keyFile);
        }
    }
}