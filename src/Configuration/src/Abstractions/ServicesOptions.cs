// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Extensions.Configuration
{
    public class ServicesOptions : AbstractServiceOptions
    {
        public ServicesOptions()
        {
        }

        public ServicesOptions(IConfigurationRoot root, string configPrefix = "")
            : base(root, configPrefix)
        {
        }

        public ServicesOptions(IConfiguration config, string configPrefix = "")
            : base(config, configPrefix)
        {
        }
    }
}
