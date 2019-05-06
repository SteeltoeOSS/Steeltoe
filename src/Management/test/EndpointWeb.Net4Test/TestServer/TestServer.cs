﻿// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Moq;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.Module;
using System;
using System.Web;

namespace Steeltoe.Management.EndpointWeb.Test
{
    [Serializable]
    public class TestServer : IDisposable
    {
        private readonly Mock<HttpContextBase> _mockContext;
        private readonly Settings _currentSettings;

        public TestServer(Settings settings = null)
        {
            _currentSettings = settings;
            _mockContext = new Mock<HttpContextBase>();
        }

        public TestHttpClient HttpClient
        {
            get
            {
#pragma warning disable CS0612 // Type or member is obsolete
                ManagementOptions.Reset();
#pragma warning restore CS0612 // Type or member is obsolete

                var handlers = ManagementConfig.ConfigureManagementActuators(null, _currentSettings);
                var actuatorModule = new ActuatorModule(handlers, null);
                return new TestHttpClient(actuatorModule, _mockContext);
            }
        }

        public void Dispose()
        {
        }
    }
}
