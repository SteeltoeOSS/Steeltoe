// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test
{
    public class ServiceBindingsTest
    {
        [Fact]
        public void NullPath()
        {
            var b = new ServiceBindingConfigurationProvider.ServiceBindings(null);
            Assert.Empty(b.Bindings);
        }

        [Fact]
        public void PopulatesContent()
        {
            var path = new PhysicalFileProvider(GetK8SResourcesDirectory());
            var b = new ServiceBindingConfigurationProvider.ServiceBindings(path);
            Assert.Equal(3, b.Bindings.Count);
        }

        private static string GetK8SResourcesDirectory()
        {
            return Path.Combine(Environment.CurrentDirectory, $"..\\..\\..\\resources\\k8s\\");
        }
    }


}
