// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;

namespace Steeltoe.Common.Security
{
    public static class PemConfigurationExtensions
    {
        public static IConfigurationBuilder AddPemFiles(this IConfigurationBuilder builder, string certFilePath, string keyFilePath)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (string.IsNullOrEmpty(certFilePath))
            {
                throw new ArgumentException(nameof(certFilePath));
            }

            if (string.IsNullOrEmpty(keyFilePath))
            {
                throw new ArgumentException(nameof(keyFilePath));
            }

            builder.Add(new PemCertificateSource(certFilePath, keyFilePath));
            return builder;
        }
    }
}
