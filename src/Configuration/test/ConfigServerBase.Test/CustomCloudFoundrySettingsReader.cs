// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    internal class CustomCloudFoundrySettingsReader : ICloudFoundrySettingsReader
    {
        public string ApplicationJson => throw new NotImplementedException();

        public string InstanceId => throw new NotImplementedException();

        public string InstanceIndex => throw new NotImplementedException();

        public string InstanceInternalIp => throw new NotImplementedException();

        public string InstanceIp => throw new NotImplementedException();

        public string InstancePort => throw new NotImplementedException();

        public string ServicesJson => throw new NotImplementedException();
    }
}
