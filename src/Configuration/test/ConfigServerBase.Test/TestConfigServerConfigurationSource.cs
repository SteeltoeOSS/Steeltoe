// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

﻿using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration.ConfigServer.Test
{
    public class TestConfigServerConfigurationSource : IConfigurationSource
    {
        private readonly IConfigurationProvider _provider;

        public TestConfigServerConfigurationSource(IConfigurationProvider provider)
        {
            _provider = provider;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder) => _provider;
    }
}
