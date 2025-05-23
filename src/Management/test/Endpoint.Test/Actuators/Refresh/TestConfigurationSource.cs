// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Management.Endpoint.Test.Actuators.Refresh;

/// <summary>
/// Tracks how many times the provider has been loaded.
/// </summary>
internal sealed class TestConfigurationSource : IConfigurationSource
{
    private readonly TestConfigurationProvider _provider = new();

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return _provider;
    }

    private sealed class TestConfigurationProvider : ConfigurationProvider
    {
        private int _loadCount;

        public override void Load()
        {
            base.Load();

            int newLoadCount = Interlocked.Increment(ref _loadCount);
            Data["FakeLoadCount"] = newLoadCount.ToString(CultureInfo.InvariantCulture);
        }
    }
}
