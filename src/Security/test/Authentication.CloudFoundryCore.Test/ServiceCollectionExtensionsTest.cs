﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Options;
using Steeltoe.Common.Security;
using System;
using Xunit;

namespace Steeltoe.Security.Authentication.CloudFoundry.Test
{
    public class ServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddCloudFoundryCertificateAuth_ChecksNulls()
        {
            // arrange
            var sColl = new ServiceCollection();

            // act & assert
            var servicesException = Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddCloudFoundryCertificateAuth(null, null));
            var configException = Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddCloudFoundryCertificateAuth(sColl, null));
            Assert.Equal("services", servicesException.ParamName);
            Assert.Equal("configuration", configException.ParamName);
        }

        [Fact]
        public void AddCloudFoundryCertificateAuth_AddsServices()
        {
            // arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            services.AddSingleton<IConfiguration>(config);

            // act
            services.AddCloudFoundryCertificateAuth(config);
            var provider = services.BuildServiceProvider();

            // assert
            Assert.NotNull(provider.GetRequiredService<IOptions<CertificateOptions>>());
            Assert.NotNull(provider.GetRequiredService<ICertificateRotationService>());
            Assert.NotNull(provider.GetRequiredService<IAuthorizationHandler>());
        }
    }
}
