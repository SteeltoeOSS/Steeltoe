// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Configuration.Kubernetes.ServiceBinding.Test
{
    public class ServiceBindingsTest
    {
        [Fact]
        public void CreateFromPath()
        {
            var rootDir = Path.Combine(Environment.CurrentDirectory, "resources\\k8s\\test-name-1");
            //var rootDir = @"D:\workspace\spring-cloud-bindings\src\test\resources\k8s\test-k8s"; // Hidden & links
            var binding = new ServiceBindingConfigurationProvider.ServiceBinding(rootDir);
            Assert.Equal("test-name-1", binding.Name);
            // Assert.Equal("test-k8s", binding.Name);
            Assert.Equal("test-type-1", binding.Type);
            Assert.Equal("test-provider-1", binding.Provider);
            Assert.Contains("resources\\k8s\\test-name-1", binding.Path);
            //Assert.Contains("resources\\k8s\\test-k8s", binding.Path);
            Assert.NotNull(binding.Secrets);
            Assert.Single(binding.Secrets);
            Assert.Equal("test-secret-value", binding.Secrets["test-secret-key"]);
        }
    }
}
