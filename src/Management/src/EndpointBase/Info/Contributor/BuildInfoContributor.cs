// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Management.Info;
using System.Diagnostics;
using System.Reflection;

namespace Steeltoe.Management.Endpoint.Info.Contributor
{
    public class BuildInfoContributor : IInfoContributor
    {
        private readonly FileVersionInfo _applicationInfo;
        private readonly FileVersionInfo _steeltoeInfo;

        public BuildInfoContributor()
        {
            _applicationInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            _steeltoeInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
        }

        public void Contribute(IInfoBuilder builder)
        {
            builder.WithInfo("applicationVersionInfo", _applicationInfo);
            builder.WithInfo("steeltoeVersionInfo", _steeltoeInfo);
        }
    }
}
