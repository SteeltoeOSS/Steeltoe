// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
#pragma warning disable CS0618 // Type or member is obsolete
                ManagementOptions.Reset();
#pragma warning restore CS0618 // Type or member is obsolete

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
