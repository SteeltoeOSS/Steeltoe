// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Kubernetes;
using Steeltoe.Management.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Steeltoe.Management.Kubernetes.Test
{
    public class ServiceCollectionExtensionsTest
    {
        [Fact]
        public void AddKubernetesInfoContributorThrowsOnNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddKubernetesInfoContributor(null));
            Assert.Equal("services", ex.ParamName);
        }

        [Fact]
        public void AddKubernetesInfoContributorAddsContributor()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            services.AddKubernetesInfoContributor();
            var contributor = services.BuildServiceProvider().GetRequiredService<IInfoContributor>();

            Assert.NotNull(contributor);
        }

        [Fact]
        public void AddKubernetesInfoContributorAddsContributorWithCustomUtilities()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

            services.AddKubernetesInfoContributor(new FakePodUtilities(null));
            var provider = services.BuildServiceProvider();
            var contributor = provider.GetRequiredService<IInfoContributor>();
            var podUtils = provider.GetRequiredService<IPodUtilities>();

            Assert.NotNull(contributor);
            Assert.NotNull(podUtils);
            Assert.IsType<FakePodUtilities>(podUtils);
        }

        [Fact]
        public void AddKubernetesActuatorsThrowsOnNull()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => ServiceCollectionExtensions.AddKubernetesInfoContributor(null));
            Assert.Equal("services", ex.ParamName);
        }

        [Fact]
        public void AddKubernetesActuators()
        {
            var services = new ServiceCollection();
            var appSettings = new Dictionary<string, string>();
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appSettings);
            services.AddSingleton<IConfiguration>(configurationBuilder.Build());
            var utils = new FakePodUtilities(FakePodUtilities.SamplePod);

            services.AddKubernetesActuators(null, utils);
            var provider = services.BuildServiceProvider();

            var infocontributors = provider.GetServices<IInfoContributor>();
            Assert.Equal(4, infocontributors.Count());
            Assert.Equal(1, infocontributors.Count(contributor => contributor.GetType().IsAssignableFrom(typeof(KubernetesInfoContributor))));
        }
    }
}
